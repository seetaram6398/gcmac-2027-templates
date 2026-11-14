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
})();
