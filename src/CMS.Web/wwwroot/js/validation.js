// jQuery-free client-side validation. ASP.NET's input tag helpers emit
// data-val-* attributes for DataAnnotations; this bridges the common ones to
// native HTML5 constraints and applies Bootstrap's validation styling on submit.
// Server-side validation (ModelState) remains authoritative.
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-val-required]').forEach(function (el) {
            el.required = true;
        });

        document.querySelectorAll('input[data-val-email]').forEach(function (el) {
            if (el.type !== 'email') el.type = 'email';
        });

        document.querySelectorAll('[data-val-length-max]').forEach(function (el) {
            const max = parseInt(el.getAttribute('data-val-length-max'), 10);
            if (!Number.isNaN(max)) el.maxLength = max;
        });

        document.querySelectorAll('[data-val-length-min]').forEach(function (el) {
            const min = parseInt(el.getAttribute('data-val-length-min'), 10);
            if (!Number.isNaN(min)) el.minLength = min;
        });

        document.querySelectorAll('form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                if (!form.checkValidity()) {
                    e.preventDefault();
                    e.stopPropagation();
                }
                form.classList.add('was-validated');
            });
        });
    });
})();
