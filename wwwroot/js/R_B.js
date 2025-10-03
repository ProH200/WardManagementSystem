// Ensure "R" prefix and validate
document.addEventListener('DOMContentLoaded', function () {
    const roomInput = document.getElementById("newRoomNumber");
    const form = roomInput.closest("form");
    const errorMessage = document.getElementById("addRoomError");

    roomInput.addEventListener("input", function () {
        if (!this.value.startsWith("R")) {
            this.value = "R" + this.value.replace(/^R*/, "");
        }
    });

    form.addEventListener("submit", function (e) {
        if (roomInput.value.trim() === "R") {
            e.preventDefault();
            errorMessage.classList.remove("d-none");
        } else {
            errorMessage.classList.add("d-none");
        }
    });
});

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
