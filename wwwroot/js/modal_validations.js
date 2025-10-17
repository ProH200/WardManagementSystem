// Add Ward Modal Validation
(function () {
    'use strict';

    // Initialize when modal is shown
    const addWardModal = document.getElementById('addWardModal');
    if (addWardModal) {
        addWardModal.addEventListener('show.bs.modal', function () {
            // Add real-time validation for ward name
            const wardNameInput = document.querySelector('#addWardForm input[name="Name"]');
            if (wardNameInput) {
                wardNameInput.addEventListener('input', validateWardName);
                wardNameInput.addEventListener('blur', validateWardName);
            }
        });

        // Clear validation when modal is hidden
        addWardModal.addEventListener('hidden.bs.modal', function () {
            const form = document.getElementById('addWardForm');
            if (form) {
                form.classList.remove('was-validated');
                form.reset();
            }

            // Remove event listeners
            const wardNameInput = document.querySelector('#addWardForm input[name="Name"]');
            if (wardNameInput) {
                wardNameInput.removeEventListener('input', validateWardName);
                wardNameInput.removeEventListener('blur', validateWardName);
            }
        });
    }

    // Ward name validation function
    function validateWardName() {
        const input = this;
        const value = input.value.trim();
        const pattern = /^[A-Za-z0-9\s\-&.,()]+$/;

        if (value.length < 2) {
            input.setCustomValidity('Ward name must be at least 2 characters long.');
        } else if (value.length > 100) {
            input.setCustomValidity('Ward name cannot exceed 100 characters.');
        } else if (!pattern.test(value)) {
            input.setCustomValidity('Ward name can only contain letters, numbers, spaces, hyphens, &, ., ,, ().');
        } else {
            input.setCustomValidity('');
        }
    }

    // Add to your existing form validation
    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
})();