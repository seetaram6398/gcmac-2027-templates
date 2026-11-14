(function () {
  "use strict";

  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  var progress = document.createElement("div");
  progress.className = "scroll-progress";
  progress.setAttribute("aria-hidden", "true");
  document.body.appendChild(progress);

  var backToTop = document.createElement("button");
  backToTop.type = "button";
  backToTop.className = "back-to-top";
  backToTop.setAttribute("aria-label", "Back to top");
  backToTop.innerHTML = "&uarr;";
  document.body.appendChild(backToTop);
  backToTop.addEventListener("click", function () {
    window.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" });
  });

  var topbar = document.querySelector(".topbar");
  var ticking = false;

  function onScroll() {
    if (ticking) return;
    ticking = true;
    requestAnimationFrame(function () {
      var root = document.documentElement;
      var max = root.scrollHeight - root.clientHeight;
      var pct = max > 0 ? (root.scrollTop / max) * 100 : 0;
      progress.style.width = pct + "%";
      if (topbar) topbar.classList.toggle("is-scrolled", root.scrollTop > 12);
      backToTop.classList.toggle("is-visible", root.scrollTop > 420);
      ticking = false;
    });
  }

  window.addEventListener("scroll", onScroll, { passive: true });
  onScroll();

  var selectors = [
    ".pillar-grid",
    ".theme-grid",
    ".about-card-grid",
    ".committee-grid",
    ".format-grid",
    ".profile-grid",
    ".contact-grid",
    ".stats",
    ".sponsor-wall",
    ".attraction-grid",
    ".venue-grid",
    ".key-dates",
    ".date-list",
    ".fees-grid",
    ".reg-callout",
    ".cta",
    ".info-grid"
  ];

  var targets = [];
  selectors.forEach(function (selector) {
    Array.prototype.forEach.call(document.querySelectorAll(selector), function (el) {
      if (!el.classList.contains("reveal")) el.classList.add("reveal");
      targets.push(el);
    });
  });

  if ("IntersectionObserver" in window && !reduceMotion) {
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add("in");
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.1, rootMargin: "0px 0px -40px 0px" });
    targets.forEach(function (el) { observer.observe(el); });
  } else {
    targets.forEach(function (el) { el.classList.add("in"); });
  }

  document.addEventListener("click", function (event) {
    if (reduceMotion) return;
    var btn = event.target.closest ? event.target.closest(".btn") : null;
    if (!btn) return;
    var rect = btn.getBoundingClientRect();
    var size = Math.max(rect.width, rect.height);
    var ripple = document.createElement("span");
    ripple.className = "btn-ripple";
    ripple.style.width = size + "px";
    ripple.style.height = size + "px";
    ripple.style.left = (event.clientX - rect.left - size / 2) + "px";
    ripple.style.top = (event.clientY - rect.top - size / 2) + "px";
    btn.appendChild(ripple);
    setTimeout(function () { ripple.remove(); }, 650);
  });

  /* Countdown timers (supports multiple per page) */
  var pad = function (n) { return n < 10 ? "0" + n : String(n); };

  Array.prototype.forEach.call(document.querySelectorAll("[data-deadline]"), function (timer) {
    var target = new Date(timer.getAttribute("data-deadline")).getTime();
    if (isNaN(target)) return;

    var fields = {
      days: timer.querySelector("[data-days]"),
      hours: timer.querySelector("[data-hours]"),
      minutes: timer.querySelector("[data-minutes]"),
      seconds: timer.querySelector("[data-seconds]")
    };

    function tick() {
      var diff = target - Date.now();
      var values = { days: 0, hours: 0, minutes: 0, seconds: 0 };
      if (diff > 0) {
        values.days = Math.floor(diff / 86400000);
        values.hours = Math.floor((diff % 86400000) / 3600000);
        values.minutes = Math.floor((diff % 3600000) / 60000);
        values.seconds = Math.floor((diff % 60000) / 1000);
      }
      Object.keys(fields).forEach(function (key) {
        if (fields[key]) fields[key].textContent = pad(values[key]);
      });
    }

    tick();
    window.setInterval(tick, 1000);
  });

  /* Add-to-calendar (.ics) */
  Array.prototype.forEach.call(document.querySelectorAll("[data-add-calendar]"), function (btn) {
    btn.addEventListener("click", function () {
      var start = btn.getAttribute("data-start");
      var end = btn.getAttribute("data-end");
      if (!start || !end) return;

      var lines = [
        "BEGIN:VCALENDAR",
        "VERSION:2.0",
        "PRODID:-//GCMAC 2027//Convention//EN",
        "CALSCALE:GREGORIAN",
        "BEGIN:VEVENT",
        "UID:gcmac-2027-" + Date.now() + "@icmai.in",
        "DTSTAMP:" + new Date().toISOString().replace(/[-:]/g, "").split(".")[0] + "Z",
        "DTSTART:" + start,
        "DTEND:" + end,
        "SUMMARY:" + (btn.getAttribute("data-title") || "GCMAC 2027"),
        "LOCATION:" + (btn.getAttribute("data-location") || "The Ashok Hotel, New Delhi"),
        "DESCRIPTION:" + (btn.getAttribute("data-details") || "Global Cost and Management Accountants' Convention 2027"),
        "END:VEVENT",
        "END:VCALENDAR"
      ];

      var blob = new Blob([lines.join("\r\n")], { type: "text/calendar;charset=utf-8" });
      var url = URL.createObjectURL(blob);
      var link = document.createElement("a");
      link.href = url;
      link.download = "gcmac-2027.ics";
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(function () { URL.revokeObjectURL(url); }, 1500);
    });
  });
})();
