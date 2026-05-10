using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    /// <summary>
    /// Employee entity. Demonstrates ENCAPSULATION.
    /// </summary>
    [Serializable]
    public class Employee
    {
        public string employeeID;
        public string name;
        public string department;
        public string contact;
        public string dateRegistered;

        public Employee() { }

        public Employee(string id, string empName, string dept, string contactInfo)
        {
            employeeID = id;
            name = empName;
            department = dept;
            contact = contactInfo;
            dateRegistered = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", employeeID, name, department);
        }
    }
}
