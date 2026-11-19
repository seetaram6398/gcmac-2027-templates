(function () {
  "use strict";

  var mount = document.querySelector("[data-dashboard-mount]");
  if (!mount) return;

  var label = mount.getAttribute("data-account-label") || "Delegate";
  var fee = mount.getAttribute("data-fee") || "As per category";
  var registrationLink = mount.getAttribute("data-registration-link") || "";
  var extraLink = mount.getAttribute("data-extra-link") || "";
  var extraLabel = mount.getAttribute("data-extra-label") || "Other delegates";

  var links = "";
  if (registrationLink) links += '<a class="btn btn-secondary btn-sm" href="' + registrationLink + '">Registration form</a>';
  if (extraLink) links += '<a class="btn btn-secondary btn-sm" href="' + extraLink + '">' + extraLabel + '</a>';

  mount.innerHTML =
    '<div class="form-shell dashboard" data-dashboard hidden>' +
      '<div class="dash-head">' +
        '<div>' +
          '<p class="card-kicker">Delegate account</p>' +
          '<h2>My GCMAC 2027</h2>' +
          '<p>Signed in as <strong>' + label + '</strong></p>' +
        '</div>' +
        '<button class="btn btn-secondary btn-sm" type="button" data-logout>Logout</button>' +
      '</div>' +
      '<div class="dash-tabs" role="tablist" aria-label="Delegate account sections">' +
        '<button class="dash-tab is-active" type="button" role="tab" aria-selected="true" data-dash-tab="registration">Registration</button>' +
        '<button class="dash-tab" type="button" role="tab" aria-selected="false" data-dash-tab="payment">Payment</button>' +
        '<button class="dash-tab" type="button" role="tab" aria-selected="false" data-dash-tab="receipt">Receipt</button>' +
      '</div>' +
      '<div class="dash-panel" data-dash-panel="registration">' +
        '<h3>Delegate registration</h3>' +
        '<p>Update your delegate details before proceeding to payment.</p>' +
        '<form class="dash-form" data-dash-register>' +
          '<div class="form-grid">' +
            '<div class="field"><label for="dash-name">Delegate\'s Name <span class="required" aria-hidden="true">*</span></label><input type="text" id="dash-name" name="delegate_name" required></div>' +
            '<div class="field"><label for="dash-designation">Designation <span class="required" aria-hidden="true">*</span></label><input type="text" id="dash-designation" name="designation" required></div>' +
            '<div class="field"><label for="dash-mobile">Mobile No. <span class="required" aria-hidden="true">*</span></label><input type="tel" id="dash-mobile" name="mobile_no" maxlength="10" pattern="[0-9]{10}" required></div>' +
            '<div class="field"><label for="dash-email">E-mail ID <span class="required" aria-hidden="true">*</span></label><input type="email" id="dash-email" name="email" required></div>' +
          '</div>' +
          '<div class="form-actions">' +
            '<button class="btn btn-primary" type="submit">Save registration</button>' +
            links +
          '</div>' +
        '</form>' +
        '<p class="dash-status" data-status role="status"></p>' +
      '</div>' +
      '<div class="dash-panel" data-dash-panel="payment" hidden>' +
        '<h3>Payment details</h3>' +
        '<table class="pay-summary">' +
          '<tbody>' +
            '<tr><th>Delegate category</th><td>' + label + '</td></tr>' +
            '<tr><th>Registration fee</th><td>' + fee + '</td></tr>' +
            '<tr><th>Payment status</th><td><span class="pay-status">Awaiting payment</span></td></tr>' +
          '</tbody>' +
        '</table>' +
        '<div class="form-actions">' +
          '<button class="btn btn-primary" type="button" data-pay-now>View payment details</button>' +
        '</div>' +
        '<p class="dash-status" data-status role="status"></p>' +
      '</div>' +
      '<div class="dash-panel" data-dash-panel="receipt" hidden>' +
        '<h3>Receipt</h3>' +
        '<p>Your GCMAC 2027 registration receipt is available to download.</p>' +
        '<div class="receipt-card">' +
          '<span class="receipt-label">GCMAC 2027 · Registration Receipt</span>' +
          '<strong>' + label + '</strong>' +
          '<span>12–14 February 2027 · The Ashok, New Delhi</span>' +
        '</div>' +
        '<div class="form-actions">' +
          '<button class="btn btn-primary" type="button" data-download-receipt>Download Receipt</button>' +
        '</div>' +
        '<p class="dash-status" data-status role="status"></p>' +
      '</div>' +
    '</div>';

  var dashboard = mount.querySelector("[data-dashboard]");
  var loginShells = Array.prototype.slice.call(document.querySelectorAll("[data-login-shell]"));

  function setStatus(message) {
    dashboard.querySelectorAll("[data-status]").forEach(function (el) { el.textContent = message; });
  }

  function showDashboard() {
    loginShells.forEach(function (shell) { shell.hidden = true; });
    dashboard.hidden = false;
    dashboard.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function showLogin() {
    dashboard.hidden = true;
    loginShells.forEach(function (shell) { shell.hidden = false; });
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  document.querySelectorAll("[data-login-form]").forEach(function (form) {
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      showDashboard();
    });
  });

  document.querySelectorAll("[data-logout]").forEach(function (button) {
    button.addEventListener("click", showLogin);
  });

  var tabs = Array.prototype.slice.call(dashboard.querySelectorAll("[data-dash-tab]"));
  tabs.forEach(function (tab) {
    tab.addEventListener("click", function () {
      tabs.forEach(function (item) {
        var active = item === tab;
        item.classList.toggle("is-active", active);
        item.setAttribute("aria-selected", String(active));
      });
      dashboard.querySelectorAll("[data-dash-panel]").forEach(function (panel) {
        panel.hidden = panel.getAttribute("data-dash-panel") !== tab.getAttribute("data-dash-tab");
      });
      setStatus("");
    });
  });

  dashboard.querySelector("[data-dash-register]").addEventListener("submit", function (event) {
    event.preventDefault();
    setStatus("Registration details saved for this demo.");
  });

  document.querySelectorAll("[data-public-register]").forEach(function (form) {
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      var status = form.parentElement.querySelector("[data-status]");
      if (status) status.textContent = "Registration details saved for this demo.";
    });
  });

  dashboard.querySelector("[data-pay-now]").addEventListener("click", function () {
    setStatus("Payment details are shown above. Online payment opens shortly.");
  });

  dashboard.querySelector("[data-download-receipt]").addEventListener("click", function () {
    var text = "GCMAC 2027 - Registration Receipt\n\n" +
      "Delegate category: " + label + "\n" +
      "Event: 12-14 February 2027, The Ashok, New Delhi\n" +
      "Fee: " + fee + "\n";
    var url = URL.createObjectURL(new Blob([text], { type: "text/plain" }));
    var link = document.createElement("a");
    link.href = url;
    link.download = "GCMAC-2027-Receipt.txt";
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    setStatus("Receipt downloaded.");
  });

  var form = document.querySelector("[data-delegate-form]");
  var updateButton = document.querySelector("[data-update-delegate]");
  var editingCell = null;

  if (updateButton) updateButton.disabled = true;

  document.addEventListener("click", function (event) {
    var button = event.target.closest("[data-edit-delegate]");
    if (!button || !form) return;
    var row = button.closest("tr");
    editingCell = row;
    row.querySelectorAll("[data-field]").forEach(function (cell) {
      var input = form.querySelector('[name="' + cell.getAttribute("data-field") + '"]');
      if (input) input.value = cell.textContent.trim() === "—" ? "" : cell.textContent.trim();
    });
    if (updateButton) updateButton.disabled = false;
    form.scrollIntoView({ behavior: "smooth", block: "start" });
  });

  function writeRow(row) {
    row.querySelectorAll("[data-field]").forEach(function (cell) {
      var input = form.querySelector('[name="' + cell.getAttribute("data-field") + '"]');
      if (input) cell.textContent = input.value || "—";
    });
    var gender = form.querySelector('[name="gender"]:checked');
    var genderCell = row.querySelector('[data-field="gender"]');
    if (gender && genderCell) genderCell.textContent = gender.value === "M" ? "Male" : "Female";
  }

  if (form && updateButton) {
    updateButton.addEventListener("click", function () {
      if (!editingCell) return;
      writeRow(editingCell);
      editingCell = null;
      updateButton.disabled = true;
      form.reset();
      var status = form.parentElement.querySelector("[data-delegate-status]");
      if (status) status.textContent = "Delegate details updated.";
    });

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      var body = document.querySelector("[data-delegate-table] tbody");
      if (!body || !body.querySelector("tr")) return;
      var row = body.querySelector("tr").cloneNode(true);
      writeRow(row);
      body.appendChild(row);
      form.reset();
      var status = form.parentElement.querySelector("[data-delegate-status]");
      if (status) status.textContent = "Delegate added.";
    });
  }
})();
