using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class CsvParser
{
    public static T[] Parse<T>(string csvText) where T : struct
    {
        var lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            Debug.LogWarning("[CsvParser] CSV 데이터가 부족함 (헤더 + 최소 1행 필요)");
            return Array.Empty<T>();
        }
        
        var headers = ParseLine(lines[0]);
        var result = new List<T>();
        var type = typeof(T);
        
        var fieldMap = new Dictionary<int, FieldInfo>();
        for (int i = 0; i < headers.Length; i++)
        {
            var field = type.GetField(headers[i], BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                fieldMap[i] = field;
            }
        }
        
        for (int row = 1; row < lines.Length; row++)
        {
            var line = lines[row].Trim();
            
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            {
                continue;
            }
            
            var values = ParseLine(line);
            object boxed = default(T);
            
            for (int col = 0; col < values.Length; col++)
            {
                if (!fieldMap.TryGetValue(col, out var field))
                {
                    continue;
                }
                
                var value = values[col].Trim();
                
                try
                {
                    object converted = ConvertValue(value, field.FieldType);
                    field.SetValue(boxed, converted);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CsvParser] 변환 실패 - 행:{row}, 열:{headers[col]}, 값:{value}, 에러:{e.Message}");
                }
            }
            
            result.Add((T)boxed);
        }
        
        return result.ToArray();
    }
    
    private static string[] ParseLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        result.Add(current.ToString());
        return result.ToArray();
    }
    
    private static object ConvertValue(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return GetDefaultValue(targetType);
        }
        
        if (targetType == typeof(string))
        {
            return value;
        }
        
        if (targetType == typeof(int))
        {
            return int.TryParse(value, out int intResult) ? intResult : 0;
        }
        
        if (targetType == typeof(float))
        {
            return float.TryParse(value, out float floatResult) ? floatResult : 0f;
        }
        
        if (targetType == typeof(double))
        {
            return double.TryParse(value, out double doubleResult) ? doubleResult : 0.0;
        }
        
        if (targetType == typeof(bool))
        {
            return value == "1" || value.ToLower() == "true";
        }
        
        if (targetType == typeof(long))
        {
            return long.TryParse(value, out long longResult) ? longResult : 0L;
        }
        
        if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, value, true, out object enumResult))
            {
                return enumResult;
            }
            return Enum.ToObject(targetType, 0);
        }
        
        return GetDefaultValue(targetType);
    }

    private static object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
}
