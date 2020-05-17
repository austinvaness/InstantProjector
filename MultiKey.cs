using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace GridSpawner
{
    public class MultiKey<T1, T2> : IEquatable<MultiKey<T1, T2>>
    {
        public T1 Key1;
        public T2 Key2;

        public MultiKey()
        {

        }

        public MultiKey (T1 key1, T2 key2)
        {
            Key1 = key1;
            Key2 = key2;
        }

        public bool Equals (MultiKey<T1, T2> other)
        {
            return other != null && 
                EqualityComparer<T1>.Default.Equals(Key1, other.Key1) &&
                EqualityComparer<T2>.Default.Equals(Key2, other.Key2);
        }

        public override bool Equals (object obj)
        {
            MultiKey<T1, T2> key = obj as MultiKey<T1, T2>;
            return key != null &&
                EqualityComparer<T1>.Default.Equals(Key1, key.Key1) &&
                EqualityComparer<T2>.Default.Equals(Key2, key.Key2);
        }

        public override int GetHashCode ()
        {
            int hashCode = 365011897;
            hashCode = hashCode * -1521134295 + EqualityComparer<T1>.Default.GetHashCode(Key1);
            hashCode = hashCode * -1521134295 + EqualityComparer<T2>.Default.GetHashCode(Key2);
            return hashCode;
        }
    }
}
