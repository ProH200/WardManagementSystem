document.addEventListener('DOMContentLoaded', function () {
    const passwordInput = document.getElementById('PasswordInput');
    const passwordHint = document.getElementById('password-hint');
    const capital = document.getElementById('capital');
    const number = document.getElementById('number');
    const special = document.getElementById('special');
    const length = document.getElementById('length');

    const specialRegex = new RegExp("[!@\\$%\\^&*()_\\-+=\\[\\]{};:'\",<.>/?\\\\|`~#]");

    // Show hint immediately on focus
    passwordInput.addEventListener('focus', function () {
        passwordHint.style.display = 'block';
    });

    // Optional: keep hint visible while typing
    passwordInput.addEventListener('input', function () {
        const val = passwordInput.value;

        capital.classList.toggle('text-success', /[A-Z]/.test(val));
        capital.classList.toggle('text-danger', !/[A-Z]/.test(val));

        number.classList.toggle('text-success', /\d/.test(val));
        number.classList.toggle('text-danger', !/\d/.test(val));

        special.classList.toggle('text-success', specialRegex.test(val));
        special.classList.toggle('text-danger', !specialRegex.test(val));

        length.classList.toggle('text-success', val.length >= 8);
        length.classList.toggle('text-danger', val.length < 8);
    });

    // Optional: hide hint when user clicks outside
    document.addEventListener('click', function (e) {
        if (!passwordInput.contains(e.target) && !passwordHint.contains(e.target)) {
            passwordHint.style.display = 'none';
        }
    });
});
