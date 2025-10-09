using System;
using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public static class ListExtensions
    {
        private static readonly Random _random = new Random();
        public static object RndFromList(this List<string> list)
        {
            if (list.Count == 0) throw new ArgumentNullException(nameof(list), "List is empty");
            int index = _random.Next(0, list.Count);
            return list[index];
            
        }
    }
    
    public static partial class ProjectExtensions
    {
        private static readonly Random _random = new Random();

        public static string RndFromList(this IZennoPosterProjectModel project, string listName, bool remove = false)
        {
            var list = project.Lists[listName];
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