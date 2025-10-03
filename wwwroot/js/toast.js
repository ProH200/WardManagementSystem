function showToast(type, message) {
    if (!message) return;
    Swal.fire({
        toast: true,
        position: 'top-end',
        icon: type,
        title: message,
        showConfirmButton: false,
        timer: 3000
    });
}
