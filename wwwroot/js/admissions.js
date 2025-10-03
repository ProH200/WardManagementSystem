//document.addEventListener("DOMContentLoaded", () => {

//    const editModal = document.getElementById('editAdmissionModal');
//    const container = document.getElementById('editAdmissionFormContainer');
//    container.classList.remove('text-center');


//    // Show modal and load form via AJAX
//    editModal.addEventListener('show.bs.modal', function (event) {
//        const button = event.relatedTarget;
//        const admissionId = button.getAttribute('data-id');

//        container.innerHTML = `
//            <div class="text-center py-5">
//                <div class="spinner-border text-primary"></div>
//                <p class="mt-3 text-muted">Loading admission details...</p>
//            </div>`;

//        fetch(`/Admission_Descharge/EditAdmission/${admissionId}`)
//            .then(res => res.text())
//            .then(html => {
//                container.innerHTML = html;

//                const form = document.getElementById('editAdmissionForm');
//                const wardSelect = form.querySelector('#WardId');
//                const roomSelect = form.querySelector('#RoomId');
//                const bedSelect = form.querySelector('#BedId');

//                // Cascading dropdowns
//                wardSelect?.addEventListener('change', function () {
//                    fetch(`/Admission_Descharge/GetRooms?wardId=${this.value}`)
//                        .then(res => res.json())
//                        .then(rooms => {
//                            roomSelect.innerHTML = '<option value="">-- Select Room --</option>';
//                            rooms.forEach(r => roomSelect.innerHTML += `<option value="${r.roomId}">${r.roomNumber}</option>`);
//                            bedSelect.innerHTML = '<option value="">-- Select Bed --</option>';
//                        });
//                });

//                roomSelect?.addEventListener('change', function () {
//                    fetch(`/Admission_Descharge/GetBeds?roomId=${this.value}`)
//                        .then(res => res.json())
//                        .then(beds => {
//                            bedSelect.innerHTML = '<option value="">-- Select Bed --</option>';
//                            beds.forEach(b => bedSelect.innerHTML += `<option value="${b.bedId}">${b.bedNumber}</option>`);
//                        });
//                });

//                // AJAX form submission
//                form.addEventListener('submit', function (e) {
//                    e.preventDefault();
//                    const formData = new FormData(form);

//                    fetch(form.action, { method: 'POST', body: formData })
//                        .then(res => res.json())
//                        .then(data => {
//                            if (data.success) {
//                                bootstrap.Modal.getInstance(editModal).hide();
//                                showToast('success', data.message);

//                                // Optional: update row in table without reloading
//                                // updateTableRow(data.updatedAdmissionId, data.htmlRow);
//                            } else {
//                                showToast('error', data.message);
//                            }
//                        })
//                        .catch(() => showToast('error', 'Failed to update admission.'));
//                });

//            })
//            .catch(() => container.innerHTML = `<div class="alert alert-danger">Failed to load form.</div>`);
//    });

//});

//// Toast function
//function showToast(type, message) {
//    if (!message) return;
//    Swal.fire({
//        toast: true,
//        position: 'top-end',
//        icon: type,
//        title: message,
//        showConfirmButton: false,
//        timer: 3000
//    });
//}
