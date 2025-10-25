using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CreateMsiFromFolder
{

    public static class ReflectionExtensions
    {
        /// <summary>
        /// Replacement for GetCustomAttribute in older .NET versions.
        /// </summary>
        /// <typeparam name="T">The type of the custom attribute to retrieve.</typeparam>
        /// <param name="member">The reflection member to inspect (Type, Method, Assembly, etc.).</param>
        /// <param name="inherit">Specifies whether to search this member's inheritance chain for attributes.</param>
        /// <returns>The custom attribute of type T, or null if not found.</returns>
        public static T GetCustomAttribute<T>(this Assembly member, bool inherit) where T : Attribute
        {
            // Retrieve all attributes of the specified type
            object[] attributes = member.GetCustomAttributes(typeof(T), inherit);
            return attributes.FirstOrDefault() as T;
        }

        /// <summary>
        /// Replacement for GetCustomAttribute in older .NET versions.
        /// </summary>
        /// <typeparam name="T">The type of the custom attribute to retrieve.</typeparam>
        /// <param name="member">The reflection member to inspect (Type, Method, Assembly, etc.).</param>
        /// <returns>The custom attribute of type T, or null if not found.</returns>
        public static T GetCustomAttribute<T>(this Assembly member) where T : Attribute
        {
            // Retrieve all attributes of the specified type
            object[] attributes = member.GetCustomAttributes(typeof(T), false);
            return attributes.FirstOrDefault() as T;
        }

    }

}
