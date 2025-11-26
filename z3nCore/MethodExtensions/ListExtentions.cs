using System;
using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public static class ListExtensions
    {
        [ThreadStatic]
        private static Random _random;
        
        private static Random Random => _random ?? (_random = new Random());

        public static T Rnd<T>(this IList<T> list, bool remove = false)
        {
            if (list.Count == 0) 
                throw new InvalidOperationException("List is empty");

            var index = Random.Next(list.Count); // ← используй property Random, не _random!
            var result = list[index];
            if (remove) list.RemoveAt(index);
            return result;
        }
    }
    
    public static partial class ProjectExtensions
    {

        public static string RndFromList(this IZennoPosterProjectModel project, string listName, bool remove = false)
        {
            var localList = project.ListSync(listName);
            
            var item = localList.Rnd(remove);
            if (remove)
                project.ListSync(listName, localList);
            return item;
            /*
            if (list.Count == 0) 
                throw new ArgumentNullException(nameof(list), "List is empty");
            
            if (!remove)
                return list[_random.Next(0, list.Count)];

            var localList = project.ListSync(listName);
            int index = _random.Next(0, localList.Count);
            var item =  localList[index];
            localList.RemoveAt(index);
            project.ListSync(listName,localList);
            return item;
            */
        }
        public static List<string> ListSync(this IZennoPosterProjectModel project, string listName)
        {
            var projectList = project.Lists[listName];
            var localList = new List<string>();
            foreach (var item in projectList)
            {
                localList.Add(item);
            }
            return localList;
            
        }
        public static List<string> ListSync(this IZennoPosterProjectModel project, string listName, List<string> localList)
        {
            var projectList = project.Lists[listName];
            projectList.Clear();
            foreach (var item in localList)
            {
                projectList.Add(item);
            }
    
            return localList;
        }
    }
}