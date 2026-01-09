// reportLoader.js - Dynamic data loader for Union Report

class ReportLoader {
    constructor() {
        this.tooltip = document.getElementById('tooltip');
        this.init();
    }

    async init() {
        this.loadData();
        this.setupTooltips();
    }

    loadData() {
        try {
            const metadata = window.reportMetadata;
            console.log('📊 metadata:', metadata);
            
            if (!metadata) {
                console.error("reportMetadata не найден!");
                return;
            }

            // 1. Отрисовка остального отчета (social + projects)
            const socialData = window.socialData;
            console.log('👥 socialData:', socialData);
            
            const projectsData = [];
            
            if (metadata.projects) {
                console.log(`🔍 Ищем ${metadata.projects.length} проектов...`);
                
                // Создаём маппинг project_* переменных (регистронезависимый)
                const projectVars = {};
                Object.keys(window).forEach(key => {
                    if (key.startsWith('project_')) {
                        projectVars[key.toLowerCase()] = key; // сохраняем оригинальный ключ
                    }
                });
                
                metadata.projects.forEach(projectName => {
                    const cleanName = projectName.replace(/[^a-zA-Z0-9]/g, '');
                    const searchKey = ('project_' + cleanName).toLowerCase();
                    
                    let projectData = null;
                    const actualKey = projectVars[searchKey];
                    
                    if (actualKey && window[actualKey]) {
                        projectData = window[actualKey];
                        console.log(`   ✅ ${projectName} -> window.${actualKey}`);
                    } else {
                        console.warn(`   ❌ ${projectName} не найден (искали ${searchKey})`);
                    }
                    
                    if (projectData) {
                        // Сохраняем оригинальное имя из metadata
                        projectData.displayName = projectName;
                        projectsData.push(projectData);
                    }
                });
                console.log(`✅ Найдено проектов: ${projectsData.length} из ${metadata.projects.length}`);
            }

            this.renderReport(socialData, projectsData, metadata);
            
            // 2. Отрисовка процессов (ПОСЛЕ основного отчета)
            this.renderProcesses();
            
            // Скрываем индикатор загрузки
            const loading = document.getElementById('loading');
            if (loading) loading.style.display = 'none';

        } catch (e) {
            console.error("❌ Ошибка загрузки данных:", e);
            const loading = document.getElementById('loading');
            if (loading) {
                loading.innerHTML = `<div style="color: #f85149;">❌ Ошибка: ${e.message}<br><small>Проверь консоль (F12)</small></div>`;
            }
        }
    }

    renderProcesses() {
        const container = document.getElementById('processMonitor');
        if (!container) {
            console.warn('⚠️ Element processMonitor not found');
            return;
        }

        console.log('🖥️ Начинаем рендеринг процессов...');
        
        // Ищем все переменные processData_*
        const processDataKeys = Object.keys(window).filter(k => k.startsWith('processData_'));
        console.log('🔍 Найдено переменных processData_*:', processDataKeys);
        
        if (processDataKeys.length === 0) {
            console.warn('⚠️ Нет данных о процессах (переменные processData_* не найдены)');
            container.innerHTML = '';
            return;
        }

        let html = '';
        processDataKeys.forEach(key => {
            const data = window[key];
            console.log(`   📦 Обрабатываем ${key}:`, data);
            
            if (!data || !data.processes) {
                console.warn(`   ⚠️ ${key} не содержит processes`);
                return;
            }

            html += `
                <div class="stats-card">
                    <h3>🖥 ${data.machineName}</h3>
                    <div class="processes-list">
                        ${data.processes.map((p, idx) => {
                            // Сохраняем данные в dataset через JS после рендеринга
                            const processId = `process_${key}_${idx}`;
                            
                            // Временно сохраняем commandLine в глобальной переменной
                            if (!window.processCommandLines) window.processCommandLines = {};
                            window.processCommandLines[processId] = p.commandLine || '';
                            
                            return `
                            <div class="process-line process-tooltip-trigger" 
                                 data-process-id="${processId}"
                                 data-process-name="${this.escapeHtml(p.name)}">
                                <span class="process-name">${p.name}</span>
                                <span class="process-mem">${p.ram}</span>
                                <span class="process-time">${p.uptime}</span>
                            </div>`;
                        }).join('')}
                    </div>
                </div>`;
        });
        
        console.log('✅ Рендеринг процессов завершен, блоков:', processDataKeys.length);
        container.innerHTML = html;
    }

    renderReport(socialData, projectsData, metadata) {
        // Update title
        const now = new Date();
        const dateStr = now.toLocaleString('ru-RU', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
        document.getElementById('reportTitle').textContent = 
            `📊 Union Report ${dateStr} id: ${metadata.userId}`;

        // Render social section
        this.renderSocialSection(socialData);

        // Render daily projects section
        this.renderDailySection(projectsData, metadata.maxAccountIndex);
    }

    renderSocialSection(socialData) {
        if (!socialData || !socialData.accounts) {
            console.warn("socialData отсутствует");
            return;
        }

        // Calculate statistics
        let stats = {
            total: socialData.accounts.length,
            twitter: { total: 0, active: 0 },
            github: { total: 0, active: 0 },
            discord: { total: 0, active: 0 },
            telegram: { total: 0, active: 0 }
        };

        socialData.accounts.forEach(acc => {
            if (acc.twitter?.login) {
                stats.twitter.total++;
                if (acc.twitter.status === 'ok') stats.twitter.active++;
            }
            if (acc.github?.login) {
                stats.github.total++;
                if (acc.github.status === 'ok') stats.github.active++;
            }
            if (acc.discord?.login) {
                stats.discord.total++;
                if (acc.discord.status === 'ok') stats.discord.active++;
            }
            if (acc.telegram?.login) {
                stats.telegram.total++;
                if (acc.telegram.status === 'ok') stats.telegram.active++;
            }
        });

        // Render summary cards
        const summaryHTML = `
            <div class="summary-card">
                <h3>TOTAL ACCOUNTS</h3>
                <div class="value">${stats.total}</div>
                <div class="subtext">Tracked accounts</div>
            </div>
            <div class="summary-card">
                <h3>TWITTER</h3>
                <div class="value" style="color: #1DA1F2;">${stats.twitter.total}</div>
                <div class="subtext">${stats.twitter.active} active</div>
            </div>
            <div class="summary-card">
                <h3>GITHUB</h3>
                <div class="value" style="color: #FFFFFF;">${stats.github.total}</div>
                <div class="subtext">${stats.github.active} active</div>
            </div>
            <div class="summary-card">
                <h3>DISCORD</h3>
                <div class="value" style="color: #5865F2;">${stats.discord.total}</div>
                <div class="subtext">${stats.discord.active} active</div>
            </div>
            <div class="summary-card">
                <h3>TELEGRAM</h3>
                <div class="value" style="color: #0088CC;">${stats.telegram.total}</div>
                <div class="subtext">${stats.telegram.active} active</div>
            </div>
        `;
        document.getElementById('socialSummary').innerHTML = summaryHTML;

        // Render heatmap
        const gridHTML = socialData.accounts.map(acc => 
            this.renderAccountCell(acc)
        ).join('');
        document.getElementById('socialGrid').innerHTML = gridHTML;
    }

    renderAccountCell(account) {
        return `
            <div class="account-cell">
                <div class="social-squares">
                    ${this.renderSocialSquare('twitter', account.twitter, account.id, '#1DA1F2')}
                    ${this.renderSocialSquare('github', account.github, account.id, '#FFFFFF')}
                    ${this.renderSocialSquare('discord', account.discord, account.id, '#5865F2')}
                    ${this.renderSocialSquare('telegram', account.telegram, account.id, '#0088CC')}
                </div>
            </div>
        `;
    }

    renderSocialSquare(socialName, social, accountId, color) {
        const hasData = social?.login;
        const isOk = social?.status === 'ok';
        
        let style = '';
        let className = 'social-square';
        
        if (!hasData) {
            style = 'background: rgba(139, 148, 158, 0.1);';
        } else if (isOk) {
            style = `background: ${color}; opacity: 0.8;`;
            className += ' active';
        } else {
            style = `background: ${color}; opacity: 0.3;`;
            className += ' inactive';
        }

        const tooltipData = `account #${accountId}||${socialName}||${social?.login || ''}||${social?.status || 'not connected'}||social`;
        
        return `<div class="${className}" style="${style}" data-tooltip="${this.escapeHtml(tooltipData)}"></div>`;
    }

    renderDailySection(projects, maxAccountIndex) {
        const container = document.getElementById('projectsGrid');
        if (!container) {
            console.error("Element 'projectsGrid' not found in HTML!");
            return;
        }

        if (!projects || projects.length === 0) {
            container.innerHTML = '<p style="color: #8b949e;">No daily projects data</p>';
            return;
        }

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Calculate overall stats
        let totalAccounts = 0;
        let totalSuccess = 0;
        let totalErrors = 0;
        
        projects.forEach(project => {
            totalAccounts += Object.keys(project.accounts).length;
            Object.values(project.accounts).forEach(acc => {
                if (acc.status === '+') totalSuccess++;
                else if (acc.status === '-') totalErrors++;
            });
        });
        
        const overallSuccessRate = totalAccounts > 0 ? ((totalSuccess / totalAccounts) * 100).toFixed(1) : '0.0';
        
        // Render summary cards
        const summaryHTML = `
            <div class="summary-card">
                <h3>TOTAL ATTEMPTS</h3>
                <div class="value">${totalAccounts}</div>
                <div class="subtext">In all projects</div>
            </div>
            <div class="summary-card">
                <h3>DONE</h3>
                <div class="value" style="color: #3fb950;">${totalSuccess}</div>
                <div class="subtext">${overallSuccessRate}% success</div>
            </div>
            <div class="summary-card">
                <h3>FAILED</h3>
                <div class="value" style="color: #f85149;">${totalErrors}</div>
                <div class="subtext">! Needs attention</div>
            </div>
        `;
        const summaryContainer = document.getElementById('dailySummary');
        if (summaryContainer) summaryContainer.innerHTML = summaryHTML;

        // Render projects
        let html = '';
        projects.forEach(project => {
            html += this.renderProjectBlock(project, maxAccountIndex, today);
        });

        container.innerHTML = html;
    }

    renderProjectBlock(project, maxIndex, today) {
        const accountIds = [];
        for (let i = 1; i <= maxIndex; i++) {
            accountIds.push(i);
        }

        // Calculate stats
        const total = Object.keys(project.accounts).length;
        const success = Object.values(project.accounts).filter(a => a.status === '+').length;
        const failed = total - success;
        const successRate = total > 0 ? ((success / total) * 100).toFixed(1) : '0.0';
        
        // Calculate timing stats
        let totalSuccessTime = 0, totalErrorTime = 0;
        let successWithTime = 0, errorWithTime = 0;
        let minSuccessTime = Infinity, maxSuccessTime = 0;
        let minErrorTime = Infinity, maxErrorTime = 0;
        
        Object.values(project.accounts).forEach(acc => {
            const time = parseFloat(acc.completionSec);
            if (!isNaN(time) && time > 0) {
                if (acc.status === '+') {
                    totalSuccessTime += time;
                    successWithTime++;
                    if (time < minSuccessTime) minSuccessTime = time;
                    if (time > maxSuccessTime) maxSuccessTime = time;
                } else {
                    totalErrorTime += time;
                    errorWithTime++;
                    if (time < minErrorTime) minErrorTime = time;
                    if (time > maxErrorTime) maxErrorTime = time;
                }
            }
        });
        
        const avgSuccessTime = successWithTime > 0 ? (totalSuccessTime / successWithTime).toFixed(1) : 0;
        const avgErrorTime = errorWithTime > 0 ? (totalErrorTime / errorWithTime).toFixed(1) : 0;
        
        // Generate cells
        const cellsHTML = accountIds.map(id => 
            this.renderProjectCell(project, id, today)
        ).join('');

        return `
            <div class="heatmap-with-stats">
                <div class="heatmap-project-card">
                    <div class="project-card">
                        <div class="project-name">${project.name}</div>
                        <div class="progress-bar">
                            <div style="display: flex; height: 100%; width: 100%;">
                                <div style="width: ${successRate}%; background: #238636;"></div>
                                <div style="width: ${(failed / total * 100).toFixed(1)}%; background: #da3633;"></div>
                            </div>
                        </div>
                        <div class="project-stats">
                            <div class="stat-row">
                                <span>✔️ Successful: </span>
                                <span class="stat-good">${success}</span>
                            </div>
                            ${successWithTime > 0 ? `
                            <div class="stat-row">
                                <span>Min|Max|Avg : </span>
                                <span class="stat-neutral">${minSuccessTime.toFixed(1)}|${maxSuccessTime.toFixed(1)}|${avgSuccessTime}s</span>
                            </div>` : ''}
                            <div class="stat-row">
                                <span>❌ Failed:  </span>
                                <span class="stat-bad">${failed}</span>
                            </div>
                            ${errorWithTime > 0 ? `
                            <div class="stat-row">
                                <span>Min|Max|Avg : </span>
                                <span class="stat-neutral">${minErrorTime.toFixed(1)}|${maxErrorTime.toFixed(1)}|${avgErrorTime}s</span>
                            </div>` : ''}
                            <div class="stat-row">
                                <span>[✔️/❌] Rate: </span>
                                <span class="${successRate >= 90 ? 'stat-good' : (successRate >= 70 ? 'stat-neutral' : 'stat-bad')}">${successRate}%</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="heatmap-content">
                    <div class="heatmap-row">
                        <div class="cells-container">
                            ${cellsHTML}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderProjectCell(project, accountId, today) {
        const acc = project.accounts[accountId];
        
        if (!acc) {
            const tooltipData = `account #${accountId}||${project.name}||—||||notdone||||daily`;
            return `<div class="heatmap-cell" data-tooltip="${this.escapeHtml(tooltipData)}"></div>`;
        }

        // Calculate age class
        const accDate = new Date(acc.timestamp);
        accDate.setHours(0, 0, 0, 0);
        const daysDiff = Math.floor((today.getTime() - accDate.getTime()) / (1000 * 60 * 60 * 24));

        let ageClass = '';
        if (daysDiff === 0) ageClass = '';
        else if (daysDiff === 1) ageClass = '-yesterday';
        else if (daysDiff === 2) ageClass = '-2days';
        else ageClass = '-old';

        const statusClass = acc.status === '+' 
            ? `success${ageClass}` 
            : `error${ageClass}`;

        const tooltipData = `account #${accountId}||${project.name}||${acc.timestamp}||${acc.completionSec || ''}||${acc.status === '+' ? 'success' : 'error'}||${acc.report || ''}||daily`;

        return `<div class="heatmap-cell ${statusClass}" data-tooltip="${this.escapeHtml(tooltipData)}"></div>`;
    }

    setupTooltips() {
        // Единый обработчик для всех тултипов (ячейки и процессы)
        document.body.addEventListener('mouseenter', (e) => {
            // Проверяем процессы
            const processLine = e.target.closest('.process-tooltip-trigger');
            if (processLine) {
                this.showProcessTooltip(e, processLine);
                return;
            }
            
            // Проверяем обычные ячейки
            const cell = e.target.closest('[data-tooltip]');
            if (cell) {
                this.showTooltip(e, cell);
            }
        }, true);

        document.body.addEventListener('mouseleave', (e) => {
            const processLine = e.target.closest('.process-tooltip-trigger');
            const cell = e.target.closest('[data-tooltip]');
            if (processLine || cell) {
                this.hideTooltip();
            }
        }, true);

        document.body.addEventListener('click', (e) => {
            // Проверяем клик на процесс
            const processLine = e.target.closest('.process-tooltip-trigger');
            if (processLine) {
                this.copyProcessCommandLine(processLine);
                return;
            }
            
            // Проверяем клик на обычную ячейку
            const cell = e.target.closest('[data-tooltip]');
            if (cell) {
                this.copyTooltipData(cell);
            }
        });
    }

    copyProcessCommandLine(processLine) {
        const processId = processLine.getAttribute('data-process-id');
        const processName = processLine.getAttribute('data-process-name');
        
        const commandLine = window.processCommandLines && window.processCommandLines[processId] 
            ? window.processCommandLines[processId] 
            : '';
        
        if (!commandLine || !commandLine.trim()) {
            console.warn('⚠️ Нет commandLine для копирования');
            return;
        }
        
        navigator.clipboard.writeText(commandLine).then(() => {
            console.log('✅ Скопировано в буфер:', commandLine.substring(0, 50) + '...');
            
            // Показываем уведомление
            const notification = document.createElement('div');
            notification.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: #238636;
                color: white;
                padding: 12px 20px;
                border-radius: 6px;
                font-size: 12px;
                z-index: 10000;
                box-shadow: 0 4px 12px rgba(0,0,0,0.3);
                animation: slideIn 0.3s ease-out;
            `;
            notification.innerHTML = `✅ Copied: <b>${processName}</b>`;
            document.body.appendChild(notification);
            
            setTimeout(() => {
                notification.style.animation = 'slideOut 0.3s ease-in';
                setTimeout(() => notification.remove(), 300);
            }, 2000);
        }).catch(err => {
            console.error('❌ Ошибка копирования:', err);
        });
    }

    showProcessTooltip(e, processLine) {
        const processId = processLine.getAttribute('data-process-id');
        const processName = processLine.getAttribute('data-process-name');
        
        // Берем commandLine из глобального хранилища
        const commandLine = window.processCommandLines && window.processCommandLines[processId] 
            ? window.processCommandLines[processId] 
            : '';
        
        console.log('🖱️ Показываем тултип процесса:', processName, 'commandLine:', commandLine.substring(0, 100) + '...');
        
        // Показываем тултип даже если commandLine пустой
        const content = commandLine && commandLine.trim()
            ? `
                <div class="tooltip-title">${processName}</div>
                <div class="tooltip-info" style="font-size: 10px; word-break: break-all; max-width: 600px; white-space: pre-wrap;">
                    ${this.escapeHtml(commandLine)}
                </div>
            `
            : `
                <div class="tooltip-title">${processName}</div>
                <div style="color: #8b949e; font-size: 11px; margin-top: 5px;">
                    ℹ️ Command line not available<br>
                    <small>Regenerate process data with updated C# code</small>
                </div>
            `;

        this.tooltip.innerHTML = content;
        this.tooltip.classList.add('show');

        // Position tooltip
        const rect = processLine.getBoundingClientRect();
        
        // Даем браузеру время отрендерить тултип перед измерением
        setTimeout(() => {
            const tooltipRect = this.tooltip.getBoundingClientRect();

            let left = rect.left + window.scrollX;
            let top = rect.top + window.scrollY - tooltipRect.height - 10;

            if (left < 10) left = 10;
            if (left + tooltipRect.width > window.innerWidth - 10) {
                left = window.innerWidth - tooltipRect.width - 10;
            }
            if (top < 10) {
                top = rect.bottom + window.scrollY + 10;
            }

            this.tooltip.style.left = left + 'px';
            this.tooltip.style.top = top + 'px';
        }, 0);
    }

    showTooltip(e, cell) {
        const data = cell.getAttribute('data-tooltip');
        if (!data) return;

        const parts = data.split('||');
        const type = parts[parts.length - 1];

        let content = '';
        if (type === 'social') {
            content = this.generateSocialTooltip(parts);
        } else {
            content = this.generateDailyTooltip(parts);
        }

        this.tooltip.innerHTML = content;
        this.tooltip.classList.add('show');

        // Position tooltip
        const rect = cell.getBoundingClientRect();
        const tooltipRect = this.tooltip.getBoundingClientRect();

        let left = rect.left + window.scrollX + rect.width / 2 - tooltipRect.width / 2;
        let top = rect.top + window.scrollY - tooltipRect.height - 10;

        if (left < 10) left = 10;
        if (left + tooltipRect.width > window.innerWidth - 10) {
            left = window.innerWidth - tooltipRect.width - 10;
        }
        if (top < 10) {
            top = rect.bottom + window.scrollY + 10;
        }

        this.tooltip.style.left = left + 'px';
        this.tooltip.style.top = top + 'px';
    }

    hideTooltip() {
        this.tooltip.classList.remove('show');
    }

    generateSocialTooltip(parts) {
        const [account, social, login, status] = parts;

        let html = `<div class="tooltip-title">${account}</div>`;
        html += `<div class="tooltip-social">${social}</div>`;

        if (login && login !== '') {
            html += `<div class="tooltip-login">${login}</div>`;
            if (status === 'ok') {
                html += '<div class="tooltip-status ok">✓ Status: OK</div>';
            } else if (status === 'not connected') {
                html += '<div class="tooltip-status empty">Not connected</div>';
            } else {
                html += `<div class="tooltip-status error">✗ Status: ${status}</div>`;
            }
        } else {
            html += '<div class="tooltip-status empty">No data</div>';
        }

        return html;
    }

    generateDailyTooltip(parts) {
        const [acc, project, time, completionTime, status, report] = parts;

        let html = `<div class="tooltip-title">${acc}</div>`;
        html += `<div style="color: #8b949e; margin-bottom: 5px;">${project}</div>`;

        if (time !== '—') {
            html += `<div class="tooltip-time">⏱ ${time}`;
            if (completionTime && completionTime !== '') {
                html += ` (${completionTime}s)`;
            }
            html += '</div>';
        }

        if (status === 'success') {
            html += '<div class="tooltip-status success">✓ Success</div>';
        } else if (status === 'error') {
            html += '<div class="tooltip-status error">✗ Failed</div>';
        } else {
            html += '<div style="color: #8b949e; font-size: 11px;">notTouched</div>';
        }

        if (report && report.trim() !== '') {
            const reportClass = status === 'error' ? 'tooltip-error' : 'tooltip-info';
            html += `<div class="${reportClass}">${report.replace(/\n/g, '<br>')}</div>`;
        }

        return html;
    }

    copyTooltipData(cell) {
        const data = cell.getAttribute('data-tooltip');
        if (!data) return;

        const parts = data.split('||');
        const type = parts[parts.length - 1];

        let copyText = '';
        if (type === 'social') {
            const [account, social, login, status] = parts;
            copyText = `${account}\n${social}`;
            if (login && login !== '') {
                copyText += `\n${login}\nStatus: ${status}`;
            } else {
                copyText += '\nNot connected';
            }
        } else {
            const [acc, project, time, completionTime, status, report] = parts;
            copyText = `${acc}\n${project}`;
            if (time !== '—') {
                copyText += `\n${time}`;
                if (completionTime && completionTime !== '') {
                    copyText += ` (${completionTime}s)`;
                }
            }
            if (status === 'success') {
                copyText += '\nStatus: Success';
            } else if (status === 'error') {
                copyText += '\nStatus: Failed';
                if (report && report.trim() !== '') {
                    copyText += `\n\nError:\n${report}`;
                }
            } else {
                copyText += '\nStatus: notTouched';
            }
        }

        navigator.clipboard.writeText(copyText).then(() => {
            const originalBorder = cell.style.border;
            cell.style.border = '2px solid #58a6ff';
            setTimeout(() => {
                cell.style.border = originalBorder;
            }, 300);
        }).catch(err => {
            console.error('Copy error:', err);
        });
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// НЕ запускаем автоматически - загрузка происходит в HTML после загрузки всех скриптов
// Экспортируем класс в window
window.ReportLoader = ReportLoader;