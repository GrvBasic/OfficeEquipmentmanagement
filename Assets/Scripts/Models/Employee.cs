using System;

namespace OEMS.Models
{
    /// <summary>
    /// Employee entity — matches the Employee table in the ER diagram.
    /// Fields: employeeId (PK), firstName, lastName, email, department.
    /// Demonstrates ENCAPSULATION.
    /// </summary>
    [Serializable]
    public class Employee
    {
        public string employeeID;
        public string firstName;
        public string lastName;
        public string email;
        public string department;
        public string dateRegistered;

        public Employee() { }

        public Employee(string id, string first, string last, string emailAddr, string dept)
        {
            employeeID     = id;
            firstName      = first;
            lastName       = last;
            email          = emailAddr;
            department     = dept;
            dateRegistered = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>Full display name as "FirstName LastName".</summary>
        public string FullName
        {
            get { return (firstName + " " + lastName).Trim(); }
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", employeeID, FullName, department);
        }
    }
}
