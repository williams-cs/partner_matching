using System;
using System.Collections.Generic;

namespace CSVUtil
{
    public class Student
    {
        public string Unix { get; set; }
        public string Notify { get; set; }
        public string Photo { get; set; }
    	public string ID { get; set; }
    	public string Name { get; set; }
    	public string GradeBasis { get; set; }
    	public string ProgramAndPlan { get; set; }
    	public string Level { get; set; }
    	public string Pronouns { get; set; }
    	public string Github { get; set; }
    	public string FirstName
    	{
    		get {
    		    var fnPlusMiddle = Name.Split(',')[1];
    		    return fnPlusMiddle.Split(' ')[0];
    		}
    	}
    	public string MiddleName
    	{
    		get {
    		    var fnPlusMiddle = Name.Split(',')[1];
    		    var fnm_arr = fnPlusMiddle.Split(' ');
    		    if (fnm_arr.Length == 1)
    		    {
    			return "";
    		    } else {	    
    		      return fnm_arr[1];
    		    }
    		}
    	}
    	public string LastName
    	{
    		get { return Name.Split(',')[0]; }
    	}	
        public string Email
        {
            get { return Unix + "@williams.edu"; }
        }
        public static Dictionary<string, Student> StudentsByID(Student[] students)
        {
            var d = new Dictionary<string, Student>();
            foreach (Student s in students)
            {
                d.Add(s.ID, s);
            }
            return d;
        }
        public static Dictionary<string, Student> StudentsByName(Student[] students)
        {
            var d = new Dictionary<string, Student>();
            foreach (Student s in students)
            {
                d.Add(s.Name, s);
            }
            return d;
        }
        public static Dictionary<string, Student> StudentsByEmail(Student[] students)
        {
            var d = new Dictionary<string, Student>();
            foreach (Student s in students)
            {
                d.Add(s.Email, s);
            }
            return d;
        }
    }

}
