using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;

namespace z3nCore
{
    public static class Running
    {
        private static readonly string _mmfName = "ZennoRunningProcesses";
        private static readonly string _mutexName = "ZennoRunningProcessesMutex";
        private static readonly int _mmfSize = 1024 * 1024; // 1MB
        private static readonly int _mutexTimeout = 5000; // 5 секунд

        // Храним ссылку на MMF для предотвращения утечки
        private static MemoryMappedFile _mmf;
        private static readonly object _mmfLock = new object();

        public static void Add(int pid, List<object> data)
        {
            ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                all[pid] = data;
                SaveAllUnsafe(all);
            });
        }

        public static List<object> Get(int pid)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                if (all.ContainsKey(pid))
                    return all[pid];
                return null;
            });
        }

        public static bool Remove(int pid)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                bool removed = all.Remove(pid);
                if (removed)
                    SaveAllUnsafe(all);
                return removed;
            });
        }

        public static bool ContainsKey(int pid)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                return all.ContainsKey(pid);
            });
        }

        public static void Clear()
        {
            ExecuteWithMutex(() => { SaveAllUnsafe(new Dictionary<int, List<object>>()); });
        }

        public static int Count
        {
            get { return ExecuteWithMutex(() => { return LoadAllUnsafe().Count; }); }
        }

        public static Dictionary<int, List<object>> ToLocal(int dataMembers = 0)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                if (dataMembers == 0)
                    return new Dictionary<int, List<object>>(all);

                return all.Where(x => x.Value.Count == dataMembers)
                    .ToDictionary(x => x.Key, x => x.Value);
            });
        }

        public static void FromLocal(Dictionary<int, List<object>> localDict)
        {
            ExecuteWithMutex(() => { SaveAllUnsafe(localDict); });
        }

        public static void PruneAndUpdate()
        {
            ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                var temp = new Dictionary<int, List<object>>();

                foreach (var process in all)
                {
                    var pid = process.Key;
                    var procData = process.Value;
                    try
                    {
                        using (var proc = Process.GetProcessById(pid))
                        {
                            var memoryMb = proc.WorkingSet64 / (1024 * 1024);
                            var runtimeMinutes = (int)(DateTime.Now - proc.StartTime).TotalMinutes;

                            var updatedData = new List<object> { memoryMb, runtimeMinutes };

                            for (int i = 2; i < procData.Count; i++)
                            {
                                updatedData.Add(procData[i]);
                            }

                            temp.Add(pid, updatedData);
                        }
                    }
                    catch
                    {
                        // dead proc - не добавляем в temp
                    }
                }

                SaveAllUnsafe(temp);
            });
        }

        // Выполнение операции под межпроцессной блокировкой
        private static void ExecuteWithMutex(Action action)
        {
            using (var mutex = new Mutex(false, _mutexName))
            {
                bool acquired = false;
                try
                {
                    acquired = mutex.WaitOne(_mutexTimeout);
                    if (!acquired)
                        throw new TimeoutException($"Failed to acquire mutex {_mutexName} within {_mutexTimeout}ms");

                    action();
                }
                finally
                {
                    if (acquired)
                        mutex.ReleaseMutex();
                }
            }
        }

        private static T ExecuteWithMutex<T>(Func<T> func)
        {
            using (var mutex = new Mutex(false, _mutexName))
            {
                bool acquired = false;
                try
                {
                    acquired = mutex.WaitOne(_mutexTimeout);
                    if (!acquired)
                        throw new TimeoutException($"Failed to acquire mutex {_mutexName} within {_mutexTimeout}ms");

                    return func();
                }
                finally
                {
                    if (acquired)
                        mutex.ReleaseMutex();
                }
            }
        }

        // Unsafe методы - должны вызываться только под мьютексом
        private static Dictionary<int, List<object>> LoadAllUnsafe()
        {
            try
            {
                EnsureMMFExists();

                using (var accessor = _mmf.CreateViewAccessor())
                {
                    byte[] buffer = new byte[_mmfSize];
                    accessor.ReadArray(0, buffer, 0, buffer.Length);

                    int length = BitConverter.ToInt32(buffer, 0);
                    if (length <= 0 || length > _mmfSize - 4)
                        return new Dictionary<int, List<object>>();

                    string json = Encoding.UTF8.GetString(buffer, 4, length);
                    var data = Global.ZennoLab.Json.JsonConvert.DeserializeObject<Dictionary<int, List<object>>>(json);
                    return data ?? new Dictionary<int, List<object>>();
                }
            }
            catch
            {
                return new Dictionary<int, List<object>>();
            }
        }

        private static void SaveAllUnsafe(Dictionary<int, List<object>> data)
        {
            try
            {
                string json = Global.ZennoLab.Json.JsonConvert.SerializeObject(data);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                if (jsonBytes.Length > _mmfSize - 4)
                    throw new Exception($"Data too large for MMF: {jsonBytes.Length} bytes (max: {_mmfSize - 4})");

                EnsureMMFExists();

                using (var accessor = _mmf.CreateViewAccessor())
                {
                    byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
                    accessor.WriteArray(0, lengthBytes, 0, 4);
                    accessor.WriteArray(4, jsonBytes, 0, jsonBytes.Length);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save data to MMF: {ex.Message}", ex);
            }
        }

        private static void EnsureMMFExists()
        {
            if (_mmf != null) return;

            lock (_mmfLock)
            {
                if (_mmf != null) return;

                try
                {
                    // Пробуем открыть существующий
                    _mmf = MemoryMappedFile.OpenExisting(_mmfName);
                }
                catch (FileNotFoundException)
                {
                    try
                    {
                        // Создаем новый
                        _mmf = MemoryMappedFile.CreateNew(_mmfName, _mmfSize);
                    }
                    catch
                    {
                        // Другой процесс успел создать - открываем
                        _mmf = MemoryMappedFile.OpenExisting(_mmfName);
                    }
                }
            }
        }

        // Метод для явной очистки ресурсов (вызывать при завершении приложения)
        public static void Dispose()
        {
            lock (_mmfLock)
            {
                if (_mmf != null)
                {
                    _mmf.Dispose();
                    _mmf = null;
                }
            }
        }
    }
    


    /// <summary>
    /// Класс для хранения информации о запущенных процессах.
    /// Данные хранятся в общей памяти, доступной всем процессам ZennoPoster.
    /// ВАЖНО: Все методы потокобезопасны и процессобезопасны.
    /// </summary>
    public static class _Running
    {
        // ============== КОНСТАНТЫ ==============
        // Имя "файла" в памяти - по этому имени все процессы находят общие данные
        private static readonly string _mmfName = "ZennoRunningProcesses";

        // Имя "замка" (mutex) - чтобы два процесса не писали одновременно
        private static readonly string _mutexName = "ZennoRunningProcessesMutex";

        // Размер "файла" в памяти - 1 мегабайт
        private static readonly int _mmfSize = 1024 * 1024;

        // Сколько секунд ждать, если другой процесс занял "замок"
        private static readonly int _mutexTimeout = 5000; // 5 секунд

        // ============== ХРАНЕНИЕ MMF ==============
        // Храним ссылку на "файл в памяти", чтобы не потерять его
        private static MemoryMappedFile _mmf;

        // Обычный замок для работы внутри одного процесса
        private static readonly object _mmfLock = new object();


        // ============== ПУБЛИЧНЫЕ МЕТОДЫ ==============

        /// <summary>
        /// Добавить информацию о процессе
        /// </summary>
        /// <param name="pid">ID процесса (например, 12345)</param>
        /// <param name="data">Данные: [память, время работы, порт, имя проекта, acc0]</param>
        public static void Add(int pid, List<object> data)
        {
            // Выполняем операцию под "замком", чтобы другие процессы подождали
            ExecuteWithMutex(() =>
            {
                // 1. Загружаем ВСЕ данные из памяти
                var all = LoadAllUnsafe();

                // 2. Добавляем/обновляем наш PID
                all[pid] = data;

                // 3. Сохраняем ВСЁ обратно в память
                SaveAllUnsafe(all);
            });
        }

        /// <summary>
        /// Получить информацию о конкретном процессе
        /// </summary>
        public static List<object> Get(int pid)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                if (all.ContainsKey(pid))
                    return all[pid];
                return null;
            });
        }

        /// <summary>
        /// Удалить процесс из списка
        /// </summary>
        public static bool Remove(int pid)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                bool removed = all.Remove(pid);
                if (removed)
                    SaveAllUnsafe(all);
                return removed;
            });
        }

        /// <summary>
        /// Проверить, есть ли процесс в списке
        /// </summary>
        public static bool ContainsKey(int pid)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                return all.ContainsKey(pid);
            });
        }

        /// <summary>
        /// Очистить ВСЕ данные
        /// </summary>
        public static void Clear()
        {
            ExecuteWithMutex(() => { SaveAllUnsafe(new Dictionary<int, List<object>>()); });
        }

        /// <summary>
        /// Сколько всего процессов в списке
        /// </summary>
        public static int Count
        {
            get { return ExecuteWithMutex(() => { return LoadAllUnsafe().Count; }); }
        }

        /// <summary>
        /// Скопировать все данные в локальный словарь (для чтения/анализа)
        /// </summary>
        public static Dictionary<int, List<object>> ToLocal(int dataMembers = 0)
        {
            return ExecuteWithMutex(() =>
            {
                var all = LoadAllUnsafe();
                if (dataMembers == 0)
                    return new Dictionary<int, List<object>>(all);

                return all.Where(x => x.Value.Count == dataMembers)
                    .ToDictionary(x => x.Key, x => x.Value);
            });
        }

        /// <summary>
        /// Записать локальный словарь в общую память (перезаписывает ВСЁ)
        /// </summary>
        public static void FromLocal(Dictionary<int, List<object>> localDict)
        {
            ExecuteWithMutex(() => { SaveAllUnsafe(localDict); });
        }

        /// <summary>
        /// Удалить "мертвые" процессы и обновить данные "живых"
        /// ВАЖНО: Этот метод "чистит" список от процессов, которые уже завершились
        /// </summary>
        public static void PruneAndUpdate()
        {
            ExecuteWithMutex(() =>
            {
                // 1. Загружаем все данные
                var all = LoadAllUnsafe();
                var temp = new Dictionary<int, List<object>>();

                // 2. Проверяем каждый процесс
                foreach (var process in all)
                {
                    var pid = process.Key;
                    var procData = process.Value;
                    try
                    {
                        // ВАЖНО: using автоматически освобождает Process после }
                        using (var proc = Process.GetProcessById(pid))
                        {
                            // Обновляем данные о процессе
                            var memoryMb = proc.WorkingSet64 / (1024 * 1024);
                            var runtimeMinutes = (int)(DateTime.Now - proc.StartTime).TotalMinutes;

                            // Создаем обновленный список данных
                            var updatedData = new List<object> { memoryMb, runtimeMinutes };

                            // Копируем остальные данные (порт, имя проекта, acc0)
                            for (int i = 2; i < procData.Count; i++)
                            {
                                updatedData.Add(procData[i]);
                            }

                            // Добавляем в новый список
                            temp.Add(pid, updatedData);
                        }
                    }
                    catch
                    {
                        // Процесс не найден = он завершился
                        // Просто не добавляем его в temp
                    }
                }

                // 3. Сохраняем только "живые" процессы
                SaveAllUnsafe(temp);
            });
        }

        /// <summary>
        /// Вызвать при завершении программы для освобождения ресурсов
        /// ОПЦИОНАЛЬНО, но рекомендуется
        /// </summary>
        public static void Dispose()
        {
            lock (_mmfLock)
            {
                if (_mmf != null)
                {
                    _mmf.Dispose();
                    _mmf = null;
                }
            }
        }


        // ============== СЛУЖЕБНЫЕ МЕТОДЫ ==============
        // Эти методы используются внутри класса

        /// <summary>
        /// ГЛАВНЫЙ МЕТОД СИНХРОНИЗАЦИИ
        /// Выполняет action под "замком", чтобы никто не мешал
        /// 
        /// ЧТО ОН ДЕЛАЕТ:
        /// 1. Создает "замок" (Mutex) - это как дверь с ключом
        /// 2. Пытается получить ключ (максимум 5 секунд ждет)
        /// 3. Если получил - выполняет action
        /// 4. Обязательно возвращает ключ в finally (даже если была ошибка)
        /// </summary>
        private static void ExecuteWithMutex(Action action)
        {
            // Создаем "замок" - он работает между всеми процессами Windows
            using (var mutex = new Mutex(false, _mutexName))
            {
                bool acquired = false; // Флаг: получили ли мы "ключ"
                try
                {
                    // Пытаемся получить "ключ" (ждем максимум 5 секунд)
                    acquired = mutex.WaitOne(_mutexTimeout);

                    if (!acquired)
                    {
                        // Не получилось получить ключ за 5 секунд
                        throw new TimeoutException(
                            $"Не смогли получить доступ к данным за {_mutexTimeout}мс. " +
                            $"Возможно, другой процесс завис."
                        );
                    }

                    // Ключ получен! Выполняем действие
                    action();
                }
                finally
                {
                    // ОБЯЗАТЕЛЬНО возвращаем ключ, если мы его брали
                    if (acquired)
                        mutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// То же самое, но для функций, которые возвращают результат
        /// </summary>
        private static T ExecuteWithMutex<T>(Func<T> func)
        {
            using (var mutex = new Mutex(false, _mutexName))
            {
                bool acquired = false;
                try
                {
                    acquired = mutex.WaitOne(_mutexTimeout);
                    if (!acquired)
                        throw new TimeoutException($"Timeout waiting for mutex {_mutexName}");

                    return func(); // Выполняем и возвращаем результат
                }
                finally
                {
                    if (acquired)
                        mutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// ЗАГРУЗКА ДАННЫХ ИЗ ПАМЯТИ
        /// ВНИМАНИЕ: Вызывать ТОЛЬКО под ExecuteWithMutex!
        /// 
        /// ЧТО ДЕЛАЕТ:
        /// 1. Открывает "файл в памяти"
        /// 2. Читает из него байты
        /// 3. Превращает байты в JSON строку
        /// 4. Превращает JSON в Dictionary
        /// </summary>
        private static Dictionary<int, List<object>> LoadAllUnsafe()
        {
            try
            {
                // Убеждаемся, что "файл в памяти" существует
                EnsureMMFExists();

                // Открываем доступ к памяти для чтения
                using (var accessor = _mmf.CreateViewAccessor())
                {
                    // 1. Создаем буфер для чтения
                    byte[] buffer = new byte[_mmfSize];

                    // 2. Читаем все байты из памяти
                    accessor.ReadArray(0, buffer, 0, buffer.Length);

                    // 3. Первые 4 байта = длина данных
                    int length = BitConverter.ToInt32(buffer, 0);

                    // 4. Проверяем корректность длины
                    if (length <= 0 || length > _mmfSize - 4)
                        return new Dictionary<int, List<object>>(); // Пусто

                    // 5. Превращаем байты в строку (начиная с позиции 4)
                    string json = Encoding.UTF8.GetString(buffer, 4, length);

                    // 6. Превращаем JSON в Dictionary
                    var data = Global.ZennoLab.Json.JsonConvert.DeserializeObject<Dictionary<int, List<object>>>(json);

                    return data ?? new Dictionary<int, List<object>>();
                }
            }
            catch
            {
                // Любая ошибка = возвращаем пустой словарь
                return new Dictionary<int, List<object>>();
            }
        }

        /// <summary>
        /// СОХРАНЕНИЕ ДАННЫХ В ПАМЯТЬ
        /// ВНИМАНИЕ: Вызывать ТОЛЬКО под ExecuteWithMutex!
        /// 
        /// ЧТО ДЕЛАЕТ:
        /// 1. Превращает Dictionary в JSON
        /// 2. Превращает JSON в байты
        /// 3. Пишет байты в "файл в памяти"
        /// </summary>
        private static void SaveAllUnsafe(Dictionary<int, List<object>> data)
        {
            try
            {
                // 1. Превращаем Dictionary в JSON строку
                string json = Global.ZennoLab.Json.JsonConvert.SerializeObject(data);

                // 2. Превращаем строку в байты
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                // 3. Проверяем, что данные влезут
                if (jsonBytes.Length > _mmfSize - 4)
                {
                    throw new Exception(
                        $"Слишком много данных: {jsonBytes.Length} байт " +
                        $"(максимум: {_mmfSize - 4} байт). " +
                        $"Нужно увеличить _mmfSize или удалить старые процессы."
                    );
                }

                // Убеждаемся, что "файл в памяти" существует
                EnsureMMFExists();

                // 4. Открываем доступ к памяти для записи
                using (var accessor = _mmf.CreateViewAccessor())
                {
                    // 5. Первые 4 байта = длина данных
                    byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
                    accessor.WriteArray(0, lengthBytes, 0, 4);

                    // 6. Остальное = сами данные
                    accessor.WriteArray(4, jsonBytes, 0, jsonBytes.Length);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения в память: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// СОЗДАНИЕ/ОТКРЫТИЕ "ФАЙЛА В ПАМЯТИ"
        /// 
        /// ЛОГИКА:
        /// - Если _mmf уже есть - ничего не делаем
        /// - Если нет - пробуем открыть существующий
        /// - Если не существует - создаем новый
        /// 
        /// ВАЖНО: Использует двойную проверку + lock для thread-safety
        /// </summary>
        private static void EnsureMMFExists()
        {
            // Быстрая проверка без lock (для производительности)
            if (_mmf != null) return;

            // Захватываем lock для безопасности
            lock (_mmfLock)
            {
                // Повторная проверка (другой поток мог создать пока мы ждали lock)
                if (_mmf != null) return;

                try
                {
                    // СЦЕНАРИЙ 1: Пробуем открыть существующий "файл"
                    _mmf = MemoryMappedFile.OpenExisting(_mmfName);
                }
                catch (FileNotFoundException)
                {
                    // СЦЕНАРИЙ 2: Не существует - пробуем создать
                    try
                    {
                        _mmf = MemoryMappedFile.CreateNew(_mmfName, _mmfSize);
                    }
                    catch
                    {
                        // СЦЕНАРИЙ 3: Другой процесс успел создать - открываем
                        _mmf = MemoryMappedFile.OpenExisting(_mmfName);
                    }
                }
            }
        }
    }
}