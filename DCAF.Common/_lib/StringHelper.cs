using System;
using System.Collections.Generic;
using System.Text;

namespace DCAF._lib
{
    public static class StringHelper
    {
        public static string ToIdentifier(this string self, IdentCasing casing = IdentCasing.None)
        {
            if (string.IsNullOrEmpty(self))
                return self;

            var processors = casing != IdentCasing.None
                ? new StringFilter<StringFilterArgs>[] { filterWhitespace, filterInitialCasing }
                : new StringFilter<StringFilterArgs>[] { filterWhitespace };
            return filter(
                new StringFilterArgs(self)
                {
                    Casing = casing,
                    RemoveAllWhitespace = true
                }, 
                processors);
        }

        public static string ToUpperInitial(this string self)
        {
            if (string.IsNullOrEmpty(self))
                return self;

            return filter(new StringFilterArgs(self)
                {
                    Casing = IdentCasing.Pascal
                },
                filterInitialCasing);
        }

        public static string ToLowerInitial(this string self)
        {
            if (string.IsNullOrEmpty(self))
                return self;

            return filter(new StringFilterArgs(self)
                {
                    Casing = IdentCasing.Lower
                },
                filterInitialCasing);
        }

        static StringFilterResult filterWhitespace<T>(T args, out char use) where T : StringFilterArgs
        {
            use = args.Array[args.Index];
            return char.IsWhiteSpace(args.Array[args.Index])
                ? StringFilterResult.Skip 
                : StringFilterResult.Continue;
        }
        
        static StringFilterResult filterInitialCasing<T>(T args, out char use) where T : StringFilterArgs
        {
            var ca = args.Array;
            var i = args.Index;
            var isInitial = args.Index == 0 || char.IsWhiteSpace(ca[i - 1]) && char.IsLetter(ca[i]);
            use = ca[i];
            if (!isInitial)
                return StringFilterResult.Continue;

            switch (args.Casing)
            {
                case IdentCasing.None:
                    return StringFilterResult.Continue;
                
                case IdentCasing.Camel:
                    // only the first letter of the string should be lower for Camel case; rest are left as-is ...  
                    if (args.GetValue("initialWasSet", false))
                    {
                        use = char.ToUpper(use);
                        return StringFilterResult.Use;
                    }

                    use = char.ToLower(use);
                    args.SetValue("initialWasSet", true);
                    return StringFilterResult.Use;
                
                case IdentCasing.Pascal:
                    use = char.ToUpper(use);
                    return StringFilterResult.Use;
                
                case IdentCasing.Kebab:
                    use = '-';
                    return StringFilterResult.Insert;
                
                case IdentCasing.Snake:
                    use = '_';
                    return StringFilterResult.Insert;
                
                case IdentCasing.Lower:
                    use = char.ToLower(use);
                    return StringFilterResult.Use;

                case IdentCasing.Upper:
                    use = char.ToUpper(use);
                    return StringFilterResult.Use;
            }
            throw new ArgumentOutOfRangeException();
        }

        static string filter<T>(T args, params StringFilter<T>[] processors) 
        where T : StringFilterArgs
        {
            const char Nope = (char)0;
            var sb = args.StringBuilder;
            var ca = args.Array;
            var i = args.Index;
            var use = (char)0;
            for (; i < ca.Length; i++)
            {
                args.Index = i;
                var doneProcessing = false;
                for (var p = 0; p < processors.Length && !doneProcessing; p++)
                {
                    var process = processors[p];
                    switch (process(args, out use))
                    {
                        case StringFilterResult.None:
                            continue;
                        
                        case StringFilterResult.Use:
                            doneProcessing = true;
                            break;
                    
                        case StringFilterResult.Insert:
                            doneProcessing = true;
                            sb.Insert(i, use);
                            use = Nope;
                            break;
                    
                        case StringFilterResult.Continue:
                            break;
                    
                        case StringFilterResult.Skip:
                            doneProcessing = true;
                            use = Nope;
                            break;
                    
                        case StringFilterResult.EndExclusive:
                            return sb.ToString();

                        case StringFilterResult.EndInclusive:
                            sb.Append(use);
                            return sb.ToString();

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (use != Nope)
                {
                    sb.Append(use);
                }                
            }

            return sb.ToString();
        }

        enum StringFilterResult
        {
            None,
            
            Use,
            
            Continue,
            
            Skip,
            
            Insert,
            
            EndExclusive,
            
            EndInclusive
        }

        delegate StringFilterResult StringFilter<in T>(T args, out char use) where T : StringFilterArgs;
        
        class StringFilterArgs
        {
            Dictionary<string, object?>? _values;

            internal StringBuilder StringBuilder { get; }
            
            public char[] Array { get; }

            public int Index { get; set; }
            
            public IdentCasing Casing { get; set; }

            public bool RemoveAllWhitespace { get; set; }
            
            public T? GetValue<T>(string key, T useDefault = default!)
            {
                if (_values is null || !_values.TryGetValue(key, out var obj) || obj is not T tv)
                    return useDefault;

                return tv;
            }
        
            public bool TryGetValue<T>(string key, out T? value)
            {
                value = default;
                if (_values is null || !_values.TryGetValue(key, out var obj) || obj is not T tv)
                    return false;

                value = tv;
                return true;
            }

            public void SetValue(string key, object? value, bool overwrite = false)
            {
                if (_values is null)
                {
                    _values = new Dictionary<string, object?> { { key, value } };
                    return;
                }

                if (!_values.TryGetValue(key, out _))
                {
                    _values.Add(key, value);
                    return;
                }

                if (!overwrite)
                    throw new Exception($"Cannot add tag '{key}' to args (was already added)");

                _values[key] = value;
            }

            
            public StringFilterArgs(string s) : this(new StringBuilder(), s.ToCharArray(), 0)
            {
            }

            public StringFilterArgs(StringBuilder stringBuilder, char[] array, int index)
            {
                StringBuilder = stringBuilder;
                Array = array;
                Index = index;
            }
        }

        public static string[] SplitAndTrim(this string self, params char[] separator)
        {
            return self.SplitAndTrim(separator, false);
        }
        
        public static string[] SplitAndTrim(this string self, char[] separator, bool removeEmptyEntries)
        {
            var flags = removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
#if NET5_0_OR_GREATER
            flags |= StringSplitOptions.TrimEntries;
            return self.Split(separator, flags);
#else
            var split = self.Split(separator, flags);
            for (var i = 0; i < split.Length; i++)
            {
                split[i] = split[i].Trim();
            }
            return split;
#endif
        }
    }
    
    public enum IdentCasing
    {
        None,
            
        Camel,
            
        Pascal,
            
        Kebab,
            
        Snake,
            
        Lower,
            
        Upper
    }
}