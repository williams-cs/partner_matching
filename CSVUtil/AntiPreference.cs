using System;
using System.Collections.Generic;

namespace CSVUtil
{
    public class AntiPreference
    {
        public string Timestamp { get; set; }
        public string EmailAddress { get; set; }
        public string IWouldPreferNotToWorkWith1 { get; set; }
        public string IWouldPreferNotToWorkWith2 { get; set; }
        public string IWouldPreferNotToWorkWith3 { get; set; }
        public string[] AsArray
        {
            get
            {
                var xs = new List<string>();
                if (!String.IsNullOrEmpty(IWouldPreferNotToWorkWith1))
                {
                    xs.Add(IWouldPreferNotToWorkWith1);
                }
                if (!String.IsNullOrEmpty(IWouldPreferNotToWorkWith2))
                {
                    xs.Add(IWouldPreferNotToWorkWith2);
                }
                if (!String.IsNullOrEmpty(IWouldPreferNotToWorkWith3))
                {
                    xs.Add(IWouldPreferNotToWorkWith3);
                }
                return xs.ToArray();
            }
        }
        public string[] AsStudentIDArray(Dictionary<string,Student> studentsByName)
        {
            string[] xs = this.AsArray;
            string[] ys = new string[xs.Length];
            for(int i = 0; i < xs.Length; i++)
            {
                var name = xs[i];

                if (studentsByName.ContainsKey(name))
                {
                    ys[i] = studentsByName[name].ID;
                }
                else
                {
                    throw new Exception("Cannot find student with name '" + name + "'! Giving up.");
                }
            }
            return ys;
        }
        public int AntiPrefCount
        {
            get { return this.AsArray.Length; }
        }

        public override string ToString()
        {
            return "[" + Timestamp + ", " + EmailAddress + ", " + IWouldPreferNotToWorkWith1 + ", " + IWouldPreferNotToWorkWith2 + ", " + IWouldPreferNotToWorkWith3;
        }
    }
}
