// Ensure "R" prefix and validate
(function () {
    'use strict';
    const form = document.getElementById('addRoomForm');
    form.addEventListener('submit', function (event) {
        const suffix = document.getElementById('RoomNumberSuffix');
        if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
        } else {
            // Create hidden full value like "R01"
            const fullRoomNumber = "R" + suffix.value;
            const hiddenInput = document.createElement("input");
            hiddenInput.type = "hidden";
            hiddenInput.name = "RoomNumber";
            hiddenInput.value = fullRoomNumber;
            form.appendChild(hiddenInput);
        }
        form.classList.add('was-validated');
    }, false);
})();

// Edit Room validation
document.addEventListener('DOMContentLoaded', function () {
    const editRoomInput = document.getElementById("editRoomNumber");
    const editRoomForm = editRoomInput?.closest("form");
    const editRoomError = document.getElementById("editRoomError");

    if (editRoomForm) {
        editRoomForm.addEventListener("submit", function (e) {
            if (editRoomInput.value.trim() === "R") {
                e.preventDefault();
                editRoomError.classList.remove("d-none");
            } else {
                editRoomError.classList.add("d-none");
            }
        });
    }

    var editRoomModal = document.getElementById('editRoomModal');
    editRoomModal.addEventListener('show.bs.modal', function (event) {
        var button = event.relatedTarget; // Button that triggered the modal
        var id = button.getAttribute('data-id');
        var number = button.getAttribute('data-number');
        var type = button.getAttribute('data-type');

        // Fill modal inputs
        document.getElementById('editRoomId').value = id;
        document.getElementById('editRoomNumber').value = number;
        document.querySelector('#editRoomModal select[name="RoomType"]').value = type;
    });


    // Bed number validation
    const newBedInput = document.getElementById("newBedNumber");
    const bedForm = newBedInput?.closest("form");
    const bedError = document.getElementById("bedError");

    if (bedForm) {
        newBedInput.addEventListener("input", function () {
            if (!this.value.startsWith("B")) {
                this.value = "B" + this.value.replace(/^B*/, "");
            }
        });

        bedForm.addEventListener("submit", function (e) {
            if (newBedInput.value.trim() === "B") {
                e.preventDefault();
                bedError.classList.remove("d-none");
            } else {
                bedError.classList.add("d-none");
            }
        });
    }
});
