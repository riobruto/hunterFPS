using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace MyEditor
{
    public static class EditorUtils
    {
        public static int[] IntArrayField(string label, ref bool open, int[] array)
        {
            // Create a foldout
            open = EditorGUILayout.Foldout(open, label);
            int newSize = array.Length;

            // Show values if foldout was opened.
            if (open)
            {
                // Int-field to set array size
                newSize = EditorGUILayout.IntField("Size", newSize);
                newSize = newSize < 0 ? 0 : newSize;

                // Creates a spacing between the input for array-size, and the array values.
                EditorGUILayout.Space();

                // Resize if user input a new array length
                if (newSize != array.Length)
                {
                    array = ResizeArray(array, newSize);
                }

                // Make multiple int-fields based on the length given
                for (var i = 0; i < newSize; ++i)
                {
                    array[i] = EditorGUILayout.IntField($"Value-{i}", array[i]);
                }
            }
            return array;
        }

        private static T[] ResizeArray<T>(T[] array, int size)
        {
            T[] newArray = new T[size];

            for (var i = 0; i < size; i++)
            {
                if (i < array.Length)
                {
                    newArray[i] = array[i];
                }
            }

            return newArray;
        }
    }
}
