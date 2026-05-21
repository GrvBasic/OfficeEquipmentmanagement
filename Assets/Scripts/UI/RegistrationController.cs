using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// REGISTRATION MODULE — Add / Remove employees.
    /// Fields match ER diagram: firstName, lastName, email, department.
    /// Also handles Remove Employee with active-assignment guard.
    /// </summary>
    public class RegistrationController : MonoBehaviour
    {
        [Header("Input Fields")]
        public TMP_InputField firstNameInput;
        public TMP_InputField lastNameInput;
        public TMP_InputField emailInput;

        [Header("Department")]
        public TMP_Dropdown   departmentDropdown;   // saved departments + a "Custom" option
        public TMP_InputField departmentInput;       // custom-entry field (shown only for "Custom")

        // Label shown for the custom option in the dropdown.
        private const string CUSTOM_OPTION = "+ Add new department…";
        private int customOptionIndex = -1;

        [Header("Display")]
        public TextMeshProUGUI generatedIdText;
        public TextMeshProUGUI emailValidationText;   // inline validation hint

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        // ── Remove-employee section (same panel, lower area) ─────────────────
        [Header("Remove Employee")]
        public TMP_InputField removeEmployeeIdInput;
        public Button         removeButton;

        private string pendingID;

        // ═════════════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            HookUpButtons();
            ResetForm();
        }

        private void HookUpButtons()
        {
            if (submitButton)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitRegistration);
            }
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => UIManager.Instance.ShowDashboard());
            }
            if (removeButton)
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(RemoveEmployee);
            }
            // Live email hint
            if (emailInput)
            {
                emailInput.onValueChanged.RemoveAllListeners();
                emailInput.onValueChanged.AddListener(OnEmailChanged);
            }
            // Department dropdown selection toggles the custom-entry field.
            if (departmentDropdown)
            {
                departmentDropdown.onValueChanged.RemoveAllListeners();
                departmentDropdown.onValueChanged.AddListener(OnDepartmentChanged);
            }
        }

        private void ResetForm()
        {
            if (firstNameInput)  firstNameInput.text  = "";
            if (lastNameInput)   lastNameInput.text   = "";
            if (emailInput)      emailInput.text      = "";
            if (departmentInput) departmentInput.text = "";
            if (emailValidationText) emailValidationText.text = "";

            PopulateDepartments();

            if (DataManager.Instance != null)
            {
                pendingID = DataManager.Instance.GenerateEmployeeID();
                if (generatedIdText) generatedIdText.text = "New ID: " + pendingID;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DEPARTMENT DROPDOWN
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Fill the dropdown with all saved departments + a trailing
        /// "Custom" option. Defaults to the first saved department, or to Custom
        /// when nothing has been saved yet.</summary>
        private void PopulateDepartments()
        {
            if (departmentDropdown == null) return;

            var dm = DataManager.Instance;
            List<string> depts = dm != null ? dm.GetDepartments() : new List<string>();

            var options = new List<string>(depts);
            options.Add(CUSTOM_OPTION);
            customOptionIndex = options.Count - 1;

            departmentDropdown.onValueChanged.RemoveListener(OnDepartmentChanged);
            departmentDropdown.ClearOptions();
            departmentDropdown.AddOptions(options);
            // No saved departments → start on Custom so the user can type one in.
            departmentDropdown.value = depts.Count > 0 ? 0 : customOptionIndex;
            departmentDropdown.RefreshShownValue();
            departmentDropdown.onValueChanged.AddListener(OnDepartmentChanged);

            UpdateCustomFieldVisibility();
        }

        private void OnDepartmentChanged(int _)
        {
            UpdateCustomFieldVisibility();
        }

        private bool IsCustomSelected()
        {
            return departmentDropdown != null &&
                   departmentDropdown.value == customOptionIndex;
        }

        /// <summary>Show the custom-entry input only when "Custom" is selected.</summary>
        private void UpdateCustomFieldVisibility()
        {
            if (departmentInput == null) return;
            bool custom = IsCustomSelected();
            departmentInput.gameObject.SetActive(custom);
            if (!custom) departmentInput.text = "";
        }

        /// <summary>Department name from the custom field (if Custom) or the
        /// selected dropdown entry otherwise.</summary>
        private string ResolveDepartment()
        {
            if (IsCustomSelected())
                return departmentInput ? departmentInput.text.Trim() : "";

            if (departmentDropdown != null &&
                departmentDropdown.value >= 0 &&
                departmentDropdown.value < departmentDropdown.options.Count)
                return departmentDropdown.options[departmentDropdown.value].text.Trim();

            return "";
        }

        // Live feedback while typing email
        private void OnEmailChanged(string value)
        {
            if (emailValidationText == null) return;
            if (string.IsNullOrEmpty(value))
            {
                emailValidationText.text = "";
                return;
            }
            emailValidationText.text = DataManager.IsValidEmail(value)
                ? "<color=#16a34a>✓ Valid email</color>"
                : "<color=#dc2626>✗ Enter a valid email (e.g. name@office.com)</color>";
        }

        // ─────────────────────────────────────────────────────────────────────
        // ADD EMPLOYEE
        // ─────────────────────────────────────────────────────────────────────
        public void SubmitRegistration()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            string first = firstNameInput  ? firstNameInput.text.Trim()  : "";
            string last  = lastNameInput   ? lastNameInput.text.Trim()   : "";
            string email = emailInput      ? emailInput.text.Trim()      : "";
            string dept  = ResolveDepartment();

            // ── Validation ──
            if (string.IsNullOrEmpty(first))
            {
                UIManager.Instance.ShowToast("First name is required.");
                return;
            }
            if (string.IsNullOrEmpty(last))
            {
                UIManager.Instance.ShowToast("Last name is required.");
                return;
            }
            if (string.IsNullOrEmpty(dept))
            {
                UIManager.Instance.ShowToast(IsCustomSelected()
                    ? "Enter the new department name."
                    : "Department is required.");
                return;
            }

            // Persist a brand-new department so it appears next time.
            if (IsCustomSelected()) dm.AddDepartment(dept);
            if (!string.IsNullOrEmpty(email) && !DataManager.IsValidEmail(email))
            {
                UIManager.Instance.ShowToast("Email format is invalid.");
                return;
            }

            var emp = new Employee(pendingID, first, last, email, dept);
            if (dm.AddEmployee(emp))
            {
                UIManager.Instance.ShowToast(
                    emp.FullName + " registered successfully! ID: " + pendingID);
                ResetForm();
            }
            else
            {
                UIManager.Instance.ShowToast("Registration failed — duplicate ID.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REMOVE EMPLOYEE
        // ─────────────────────────────────────────────────────────────────────
        public void RemoveEmployee()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            string eid = removeEmployeeIdInput ? removeEmployeeIdInput.text.Trim() : "";
            if (string.IsNullOrEmpty(eid))
            {
                UIManager.Instance.ShowToast("Enter an Employee ID to remove.");
                return;
            }

            var emp = dm.FindEmployee(eid);
            if (emp == null)
            {
                UIManager.Instance.ShowToast("Employee " + eid + " not found.");
                return;
            }

            // Show confirm dialog; actual deletion happens in OnConfirmRemove
            UIManager.Instance.ShowConfirm(
                "Remove " + emp.FullName + "?\nThis cannot be undone.",
                () => OnConfirmRemove(eid, emp.FullName));
        }

        private void OnConfirmRemove(string eid, string fullName)
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (dm.RemoveEmployee(eid))
            {
                UIManager.Instance.ShowToast(fullName + " removed.");
                if (removeEmployeeIdInput) removeEmployeeIdInput.text = "";
            }
            else
            {
                UIManager.Instance.ShowToast(
                    "Cannot remove " + fullName + " — they have active assignments.");
            }
        }
    }
}
