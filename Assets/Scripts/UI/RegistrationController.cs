using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// REGISTRATION MODULE controller (per synopsis section 9.1).
    /// </summary>
    public class RegistrationController : MonoBehaviour
    {
        [Header("Input Fields")]
        public TMP_InputField nameInput;
        public TMP_InputField departmentInput;
        public TMP_InputField contactInput;

        [Header("Display")]
        public TextMeshProUGUI generatedIdText;

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        private string pendingID;

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
        }

        private void ResetForm()
        {
            if (nameInput)       nameInput.text = "";
            if (departmentInput) departmentInput.text = "";
            if (contactInput)    contactInput.text = "";

            // pre-generate a preview of next id
            if (DataManager.Instance != null)
            {
                pendingID = DataManager.Instance.GenerateEmployeeID();
                if (generatedIdText) generatedIdText.text = "ID: " + pendingID;
            }
        }

        public void SubmitRegistration()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            string n = nameInput ? nameInput.text.Trim() : "";
            string d = departmentInput ? departmentInput.text.Trim() : "";
            string c = contactInput ? contactInput.text.Trim() : "";

            if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(d))
            {
                UIManager.Instance.ShowToast("Name and Department are required.");
                return;
            }

            var emp = new Employee(pendingID, n, d, c);
            if (dm.AddEmployee(emp))
            {
                UIManager.Instance.ShowToast("Employee " + n + " registered (" + pendingID + ")");
                ResetForm();
            }
            else
            {
                UIManager.Instance.ShowToast("Registration failed (duplicate ID).");
            }
        }
    }
}
