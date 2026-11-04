(function () {
  "use strict";

  var root = document.documentElement;

  /* ---------------- Theme toggle ---------------- */
  var themeToggle = document.getElementById("themeToggle");
  var storedTheme = null;

  try {
    storedTheme = localStorage.getItem("gcmac-theme");
  } catch (err) {
    storedTheme = null;
  }

  if (storedTheme === "dark" || storedTheme === "light") {
    root.setAttribute("data-theme", storedTheme);
  }

  function syncThemeLabel() {
    if (!themeToggle) return;
    var isDark = root.getAttribute("data-theme") === "dark";
    themeToggle.setAttribute(
      "aria-label",
      isDark ? "Switch to light theme" : "Switch to dark theme"
    );
  }

  if (themeToggle) {
    syncThemeLabel();
    themeToggle.addEventListener("click", function () {
      var next = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
      root.setAttribute("data-theme", next);
      try {
        localStorage.setItem("gcmac-theme", next);
      } catch (err) {
        /* storage may be unavailable */
      }
      syncThemeLabel();
    });
  }

  /* ---------------- Sticky header state ---------------- */
  var header = document.getElementById("siteHeader");

  function updateHeader() {
    if (!header) return;
    header.classList.toggle("is-scrolled", window.scrollY > 8);
  }

  updateHeader();
  window.addEventListener("scroll", updateHeader, { passive: true });

  /* ---------------- Mobile navigation ---------------- */
  var nav = document.getElementById("primaryNav");
  var navToggle = document.getElementById("navToggle");

  function setNav(open) {
    if (!nav || !navToggle) return;
    nav.classList.toggle("is-open", open);
    navToggle.setAttribute("aria-expanded", String(open));
    navToggle.setAttribute("aria-label", open ? "Close menu" : "Open menu");
    document.body.classList.toggle("nav-open", open);
  }

  if (navToggle) {
    navToggle.addEventListener("click", function () {
      setNav(navToggle.getAttribute("aria-expanded") !== "true");
    });
  }

  if (nav) {
    nav.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () {
        setNav(false);
      });
    });
  }

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") setNav(false);
  });

  window.addEventListener("resize", function () {
    if (window.innerWidth > 900) setNav(false);
  });

  /* ---------------- Countdown ---------------- */
  var countdown = document.getElementById("countdown");
  var target = new Date("2027-02-12T09:00:00+05:30").getTime();

  function pad(value) {
    return value < 10 ? "0" + value : String(value);
  }

  function updateCountdown() {
    if (!countdown) return;

    var diff = target - Date.now();

    if (diff <= 0) {
      countdown.querySelectorAll("[data-days], [data-hours], [data-minutes], [data-seconds]").forEach(function (el) {
        el.textContent = "00";
      });
      return;
    }

    var days = Math.floor(diff / 86400000);
    var hours = Math.floor((diff % 86400000) / 3600000);
    var minutes = Math.floor((diff % 3600000) / 60000);
    var seconds = Math.floor((diff % 60000) / 1000);

    var d = countdown.querySelector("[data-days]");
    var h = countdown.querySelector("[data-hours]");
    var m = countdown.querySelector("[data-minutes]");
    var s = countdown.querySelector("[data-seconds]");

    if (d) d.textContent = pad(days);
    if (h) h.textContent = pad(hours);
    if (m) m.textContent = pad(minutes);
    if (s) s.textContent = pad(seconds);
  }

  updateCountdown();
  window.setInterval(updateCountdown, 1000);

  /* ---------------- Programme tabs ---------------- */
  var tabs = Array.prototype.slice.call(document.querySelectorAll(".tab"));

  function activateTab(tab) {
    tabs.forEach(function (item) {
      var isActive = item === tab;
      var panel = document.getElementById(item.getAttribute("data-day"));

      item.classList.toggle("is-active", isActive);
      item.setAttribute("aria-selected", String(isActive));
      item.tabIndex = isActive ? 0 : -1;

      if (panel) {
        panel.hidden = !isActive;
        panel.classList.toggle("is-active", isActive);
      }
    });
  }

  tabs.forEach(function (tab, index) {
    tab.addEventListener("click", function () {
      activateTab(tab);
    });

    tab.addEventListener("keydown", function (event) {
      var nextIndex = null;

      if (event.key === "ArrowRight" || event.key === "ArrowDown") {
        nextIndex = (index + 1) % tabs.length;
      } else if (event.key === "ArrowLeft" || event.key === "ArrowUp") {
        nextIndex = (index - 1 + tabs.length) % tabs.length;
      } else if (event.key === "Home") {
        nextIndex = 0;
      } else if (event.key === "End") {
        nextIndex = tabs.length - 1;
      }

      if (nextIndex !== null) {
        event.preventDefault();
        activateTab(tabs[nextIndex]);
        tabs[nextIndex].focus();
      }
    });
  });

  /* ---------------- Scroll reveal ---------------- */
  var revealItems = Array.prototype.slice.call(document.querySelectorAll(".reveal"));

  if ("IntersectionObserver" in window) {
    var observer = new IntersectionObserver(
      function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            entry.target.classList.add("in");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12, rootMargin: "0px 0px -40px 0px" }
    );

    revealItems.forEach(function (item, index) {
      item.style.transitionDelay = (index % 3) * 70 + "ms";
      observer.observe(item);
    });
  } else {
    revealItems.forEach(function (item) {
      item.classList.add("in");
    });
  }
})();
