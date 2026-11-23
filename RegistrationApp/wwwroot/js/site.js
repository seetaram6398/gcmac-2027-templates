document.addEventListener("DOMContentLoaded", function () {
    var memberNumber = document.querySelector('input[inputmode="numeric"][pattern="[0-9]*"]');
    if (memberNumber) {
        memberNumber.addEventListener("input", function () {
            memberNumber.value = memberNumber.value.replace(/\D/g, "");
        });
    }

    var gstToggle = document.querySelector('input[name="Input.GstDetailsRequired"]');
    var gstinField = document.querySelector('input[name="Input.Gstin"]');
    if (gstToggle && gstinField) {
        var syncGst = function () {
            gstinField.disabled = !gstToggle.checked;
            gstinField.closest("label").style.opacity = gstToggle.checked ? "1" : "0.5";
        };
        gstToggle.addEventListener("change", syncGst);
        syncGst();
    }

    var memberSidebarToggle = document.querySelector("[data-member-sidebar-toggle]");
    if (memberSidebarToggle) {
        memberSidebarToggle.addEventListener("click", function () {
            var sidebar = memberSidebarToggle.closest(".member-sidebar");
            if (!sidebar) return;
            var open = sidebar.classList.toggle("open");
            memberSidebarToggle.setAttribute("aria-expanded", open ? "true" : "false");
        });
    }

    document.querySelectorAll("form").forEach(function (form) {
        if (!form.querySelector('input[name="Method"]')) return;
        form.addEventListener("submit", function (event) {
            var selected = form.querySelector('input[name="Method"]:checked');
            var reference = form.querySelector('input[name="Reference"]');
            if (selected && selected.value === "Offline") {
                if (!reference || !reference.value.trim()) {
                    event.preventDefault();
                    window.alert("Enter the payment reference or UTR number for offline payment.");
                }
                return;
            }
            if (!window.confirm("You will be taken to the payment gateway. Continue?")) {
                event.preventDefault();
            }
        });
    });
});
