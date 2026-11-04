(function () {
  "use strict";

  /* Mobile navigation */
  var toggle = document.querySelector(".nav-toggle");
  var nav = document.querySelector(".site-nav");

  function setNav(open) {
    if (!toggle || !nav) return;
    nav.classList.toggle("open", open);
    toggle.setAttribute("aria-expanded", String(open));
    toggle.setAttribute("aria-label", open ? "Close menu" : "Open menu");
    document.body.classList.toggle("menu-open", open);
  }

  if (toggle && nav) {
    toggle.addEventListener("click", function () {
      setNav(toggle.getAttribute("aria-expanded") !== "true");
    });
    nav.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () { setNav(false); });
    });
    document.addEventListener("keydown", function (event) {
      if (event.key === "Escape") setNav(false);
    });
    window.addEventListener("resize", function () {
      if (window.innerWidth > 860) setNav(false);
    });
  }

  /* Optional theme switch */
  var themeToggle = document.querySelector("[data-theme-toggle]");
  if (themeToggle) {
    var root = document.documentElement;
    var saved = null;
    try { saved = localStorage.getItem("gcmac-theme"); } catch (err) { saved = null; }
    if (saved === "dark" || saved === "light") root.setAttribute("data-theme", saved);

    themeToggle.addEventListener("click", function () {
      var next = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
      root.setAttribute("data-theme", next);
      try { localStorage.setItem("gcmac-theme", next); } catch (err) { /* noop */ }
    });
  }

  /* Countdown */
  var countdown = document.querySelector(".countdown");
  if (countdown) {
    var target = new Date("2027-02-12T09:00:00+05:30").getTime();
    var pad = function (n) { return n < 10 ? "0" + n : String(n); };
    var tick = function () {
      var diff = target - Date.now();
      var values = { days: 0, hours: 0, minutes: 0, seconds: 0 };
      if (diff > 0) {
        values.days = Math.floor(diff / 86400000);
        values.hours = Math.floor((diff % 86400000) / 3600000);
        values.minutes = Math.floor((diff % 3600000) / 60000);
        values.seconds = Math.floor((diff % 60000) / 1000);
      }
      Object.keys(values).forEach(function (key) {
        var el = countdown.querySelector("[data-" + key + "]");
        if (el) el.textContent = pad(values[key]);
      });
    };
    tick();
    window.setInterval(tick, 1000);
  }

  /* Programme tabs */
  var tabs = Array.prototype.slice.call(document.querySelectorAll("[data-tab]"));
  if (tabs.length) {
    tabs.forEach(function (tab) {
      tab.addEventListener("click", function () {
        var targetId = tab.getAttribute("data-tab");
        tabs.forEach(function (item) {
          var active = item === tab;
          item.classList.toggle("is-active", active);
          item.setAttribute("aria-selected", String(active));
          var panel = document.getElementById(item.getAttribute("data-tab"));
          if (panel) {
            panel.hidden = !active;
            panel.classList.toggle("is-active", active);
          }
        });
        var activePanel = document.getElementById(targetId);
        if (activePanel) activePanel.hidden = false;
      });
    });
  }

  /* Reveal on scroll */
  var items = Array.prototype.slice.call(document.querySelectorAll(".reveal"));
  if ("IntersectionObserver" in window && items.length) {
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add("in");
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12, rootMargin: "0px 0px -30px 0px" });
    items.forEach(function (item) { observer.observe(item); });
  } else {
    items.forEach(function (item) { item.classList.add("in"); });
  }
})();
