/// <copyright file="OtoMapper.cs" company="Sysco">
/// Copyright (c) 2013 All Rights Reserved
/// </copyright>
/// <author>Kevin Wong</author>
/// <date>12/23/2013 10:39:58 AM </date>
/// <summary>Object to object mapper</summary>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace KWUtils
{
    /// <summary>
    /// Object to Object Mapper:
    /// Useful to map similar structured objects with different application fields
    /// Example: Domain Entity to UI Entity
    /// 
    /// This includes <b>four scenarios:</b>
    /// - Mapping exact property names: Property = Property
    /// - Mapping source child property to destination property: Child.Property = ChildProperty
    /// - Mapping source method to destination property: GetProperty() = Property
    /// - Optional Custom Mappings specified at instantiation time
    /// </summary>
    public class OtoMapper
    {
        /// <summary>
        /// Initializes with custom mappings
        /// </summary>
        /// <param name="customMappings">Specifies a dictionary of (destination, source) property names to override
        /// default mapping behavior for <b>those properties only</b></param>


        public static void MapListIntoObservableCollection<T, R>(IEnumerable<T> sourceList, ObservableCollection<R> destinationList)
        {
            MapListIntoObservableCollection<T, R>(sourceList, destinationList, null);
        }

        /// <summary>
        /// Maps a source enumerable into an observable collection of specified Type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceList"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public static void MapListIntoObservableCollection<T, R>(IEnumerable<T> sourceList, ObservableCollection<R> destinationList, 
            Dictionary<string, string> customMappings)
        {
            foreach (T sourceObject in sourceList)
            {
                R destinationObject = (R)Activator.CreateInstance(typeof(R));
                MapSingleEntities(sourceObject, destinationObject, customMappings);
                destinationList.Add(destinationObject);
            }
        }

        public static void MapSingleEntities(object source, object destination)
        {
            MapSingleEntities(source, destination, null);
        }

        /// <summary>
        /// Maps source object properties into destination object
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void MapSingleEntities(object source, object destination, Dictionary<string, string> customMappings)
        {
            PropertyInfo[] destinationProperties = destination.GetType().GetProperties();
            PropertyInfo[] sourceProperties = source.GetType().GetProperties();
            MethodInfo[] sourceMethods = source.GetType().GetMethods();

            foreach (PropertyInfo destinationProperty in destinationProperties)
            {
                string destinationPropertyName = destinationProperty.Name;

                // CustomMapping
                if (customMappings != null && customMappings.ContainsKey(destinationPropertyName))
                {
                    string predefinedSourcePropertyName;
                    customMappings.TryGetValue(destinationPropertyName, out predefinedSourcePropertyName);
                    handlePropertyMatching(predefinedSourcePropertyName, destinationPropertyName, source, destination);
                    continue;
                }

                // Property
                string sourcePropertyName = getMatchingCurrentPropertyName(destinationPropertyName, sourceProperties);
                if (!sourcePropertyName.Equals(""))
                {
                    handlePropertyMatching(sourcePropertyName, destinationPropertyName, source, destination);
                    continue;
                }

                // Child Property
                KeyValuePair<string, string> sourceChildNameAndProperty = getMatchingChildPropertyName(destinationPropertyName, sourceProperties, source);
                if (!sourceChildNameAndProperty.Key.Equals(""))
                {
                    handleChildPropertyToPropertyMatching(sourceChildNameAndProperty.Key, sourceChildNameAndProperty.Value, destinationPropertyName, source, destination);
                    continue;
                }

                // Method
                string sourceMethodName = getMatchingMethodName(destinationPropertyName, sourceMethods);
                if (!sourceMethodName.Equals(""))
                {
                    handleMethodToPropertyMatching(sourceMethodName, destinationPropertyName, source, destination);
                    continue;
                }
            }
        }

        /// <summary>
        /// Finds a matching source property name to destination property
        /// </summary>
        private static string getMatchingCurrentPropertyName(string destinationPropertyName, PropertyInfo[] sourceProperties)
        {
            foreach (PropertyInfo sourceProperty in sourceProperties)
            {
                string sourcePropertyName = sourceProperty.Name;
                if (destinationPropertyName.Equals(sourcePropertyName))
                    return sourcePropertyName;
            }

            return "";
        }

        private static KeyValuePair<string, string> getMatchingChildPropertyName<T>(string destinationPropertyName, PropertyInfo[] sourceProperties, T source)
        {
            int splitIndex = getPropertyCamelCaseSplitIndex(destinationPropertyName);
            if (splitIndex != 0)
            {
                string desiredChildObject = destinationPropertyName.Substring(0, splitIndex);
                string desiredChildProperty = destinationPropertyName.Substring(splitIndex);

                foreach (PropertyInfo sourceProperty in sourceProperties)
                {
                    object childObject = sourceProperty.GetValue(source) as object;
                    if (childObject == null)
                        continue;

                    PropertyInfo[] childProperties = childObject.GetType().GetProperties();
                    string matchingChildProperty = getMatchingCurrentPropertyName(desiredChildProperty, childProperties);
                    if (matchingChildProperty != "")
                        return new KeyValuePair<string, string>(desiredChildObject, desiredChildProperty);
                }
            }

            // No matching child property was found
            return new KeyValuePair<string,string>("", "");
        }

        private static int getPropertyCamelCaseSplitIndex(string input)
        {
            bool lowerCaseCharacterFound = false;
            int inputLooperCount = 0;

            foreach (char inputCharacter in input)
            {
                if (char.IsLower(inputCharacter))
                    lowerCaseCharacterFound = true;

                if (char.IsUpper(inputCharacter) && lowerCaseCharacterFound)
                    return inputLooperCount;
            }

            return inputLooperCount;
        }

        /// <summary>
        /// Finds a matching source method name to destination property
        /// </summary>
        private static string getMatchingMethodName(string destinationPropertyName, MethodInfo[] sourceMethods)
        {
            foreach (MethodInfo sourceMethod in sourceMethods)
            {
                string sourceMethodName = sourceMethod.Name;
                if (sourceMethodName.Equals("Get" + destinationPropertyName))
                    return sourceMethodName;
            }

            // No matching method was found
            return "";
        }

        private static void handlePropertyMatching(string sourcePropertyName, string destinationPropertyName, object sourceObject, object destinationObject)
        {
            object sourceObjectValue = sourceObject.GetType().GetProperty(sourcePropertyName).GetValue(sourceObject);
            destinationObject.GetType().GetProperty(destinationPropertyName).SetValue(destinationObject, sourceObjectValue);
        }

        private static void handleChildPropertyToPropertyMatching(string sourceChildName, string childPropertyName, string destinationPropertyName, object sourceObject, object destinationObject)
        {
            object childObject = sourceObject.GetType().GetProperty(sourceChildName).GetValue(sourceObject);
            object childPropertyValue = childObject.GetType().GetProperty(childPropertyName).GetValue(childObject);
            destinationObject.GetType().GetProperty(destinationPropertyName).SetValue(destinationObject, childPropertyValue);
        }

        private static void handleMethodToPropertyMatching(string sourceMethodName, string destinationPropertyName, object sourceObject, object destinationObject)
        {
            MethodInfo sourceMethod = sourceObject.GetType().GetMethod(sourceMethodName);
            if (sourceMethod.GetParameters().Count() > 0)
                return;
            object sourceMethodResult = sourceObject.GetType().GetMethod(sourceMethodName).Invoke(sourceObject, null);
            destinationObject.GetType().GetProperty(destinationPropertyName).SetValue(destinationObject, sourceMethodResult);
        }

    }
}
