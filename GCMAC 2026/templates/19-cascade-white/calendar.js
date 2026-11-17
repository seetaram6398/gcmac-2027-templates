/* Events calendar for 19 · Cascade White (adapted from global.icai.org) */
(function () {
  var events = [
    {
      date: new Date(2026, 11, 15),
      title: "Early Bird registration closes",
      type: "Early Bird",
      color: "#F1883A",
      desc: "Register before 15 December 2026 to avail the Early Bird delegate fee."
    },
    {
      date: new Date(2027, 1, 12),
      title: "Inaugural Plenary & Global Perspective",
      type: "Convention",
      color: "#3D4785",
      desc: "Registration, inauguration, Global Perspective on CMAs in Governance, high tea and networking."
    },
    {
      date: new Date(2027, 1, 13),
      title: "Parallel Technical Sessions & CXO Fireside",
      type: "Convention",
      color: "#1868A8",
      desc: "Parallel technical sessions, CXO fireside and plenary panel, followed by a networking dinner."
    },
    {
      date: new Date(2027, 1, 14),
      title: "Technical Session V & Valedictory Session",
      type: "Convention",
      color: "#26A951",
      desc: "Vision 2030, panel discussion, valedictory session and networking lunch before departure."
    }
  ];

  function key(d) {
    return d.getFullYear() + "-" + d.getMonth() + "-" + d.getDate();
  }

  function eventsFor(d) {
    var k = key(d);
    var found = [];
    events.forEach(function (ev) {
      if (key(ev.date) === k) found.push(ev);
    });
    return found;
  }

  function firstRelevantMonth() {
    var now = new Date();
    var current = new Date(now.getFullYear(), now.getMonth(), 1);
    var sorted = events.slice().sort(function (a, b) { return a.date - b.date; });
    for (var i = 0; i < sorted.length; i++) {
      var m = new Date(sorted[i].date.getFullYear(), sorted[i].date.getMonth(), 1);
      if (m.getTime() >= current.getTime()) return m;
    }
    if (sorted.length) return new Date(sorted[0].date.getFullYear(), sorted[0].date.getMonth(), 1);
    return current;
  }

  function formatDate(d) {
    return d.toLocaleString("default", { day: "numeric", month: "long", year: "numeric" });
  }

  function setup(root) {
    var grid = root.querySelector(".cal-grid");
    var title = root.querySelector(".cal-title");
    var prevBtn = root.querySelector(".cal-prev");
    var nextBtn = root.querySelector(".cal-next");
    var detail = root.querySelector(".cal-detail");
    var tooltip = root.querySelector(".cal-tooltip");
    if (!grid || !title) return;

    var selectedKey = null;
    var tooltipTimer = null;
    var view = firstRelevantMonth();

    function hideTooltip() {
      if (tooltipTimer) clearTimeout(tooltipTimer);
      if (tooltip) tooltip.classList.remove("show");
    }

    function positionTooltip(x, y) {
      if (!tooltip) return;
      var rect = tooltip.getBoundingClientRect();
      var left = x + 14;
      var top = y - 10;
      if (left + rect.width > window.innerWidth - 10) left = x - rect.width - 14;
      if (top + rect.height > window.innerHeight - 10) top = window.innerHeight - rect.height - 10;
      if (top < 10) top = 10;
      tooltip.style.left = left + "px";
      tooltip.style.top = top + "px";
    }

    function showTooltip(e, evs) {
      if (!tooltip || !evs.length) return;
      hideTooltip();
      var html = "";
      evs.forEach(function (ev, i) {
        if (i > 0) html += '<hr style="border:none;border-top:1px solid rgba(255,255,255,.12);margin:6px 0">';
        html += '<div class="cal-tt-title">' + ev.title + "</div>";
        html += '<div class="cal-tt-sub"><span style="display:inline-block;width:8px;height:8px;border-radius:50%;background:' + ev.color + ';flex-shrink:0"></span>' + ev.type + " &middot; " + formatDate(ev.date) + "</div>";
      });
      tooltip.innerHTML = html;
      tooltipTimer = setTimeout(function () {
        positionTooltip(e.clientX, e.clientY);
        tooltip.classList.add("show");
      }, 180);
    }

    function showDetail(evs) {
      if (!detail) return;
      if (!evs.length) {
        detail.innerHTML = '<span class="cal-detail-empty">Select a highlighted date to see details.</span>';
        return;
      }
      detail.innerHTML = evs.map(function (ev) {
        return '<strong>' + formatDate(ev.date) + '</strong>' + ev.title + ' &mdash; ' + ev.desc;
      }).join('<br><br>');
    }

    function render() {
      var y = view.getFullYear();
      var m = view.getMonth();
      var firstDay = new Date(y, m, 1);
      var startPad = firstDay.getDay() === 0 ? 6 : firstDay.getDay() - 1;
      var totalDays = new Date(y, m + 1, 0).getDate();

      title.textContent = firstDay.toLocaleString("default", { month: "long", year: "numeric" });

      var today = new Date();
      today.setHours(0, 0, 0, 0);

      var html = "";
      for (var i = 0; i < startPad; i++) {
        html += '<div class="cal-day other-month"></div>';
      }
      for (var d = 1; d <= totalDays; d++) {
        var date = new Date(y, m, d);
        var cls = "cal-day";
        if (date.toDateString() === today.toDateString()) cls += " today";
        if (date < today) cls += " past";
        var evs = eventsFor(date);
        if (evs.length) cls += " has-event";
        var k = key(date);
        if (selectedKey === k) cls += " selected";

        html += '<div class="' + cls + '" data-date="' + k + '">';
        html += '<span class="cal-day-num">' + d + "</span>";
        if (evs.length) {
          html += '<div class="cal-ev-list">';
          evs.slice(0, 2).forEach(function (ev) {
            html += '<span class="cal-ev-item" style="background:' + ev.color + '">' + ev.title + "</span>";
          });
          if (evs.length > 2) {
            html += '<span class="cal-ev-more">+' + (evs.length - 2) + " more</span>";
          }
          html += "</div>";
        }
        html += "</div>";
      }

      grid.innerHTML = html;

      Array.prototype.forEach.call(grid.querySelectorAll(".cal-day.has-event"), function (el) {
        var parts = el.getAttribute("data-date").split("-");
        var date = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10), parseInt(parts[2], 10));
        var evs = eventsFor(date);

        el.addEventListener("mouseenter", function (e) { showTooltip(e, evs); });
        el.addEventListener("mousemove", function (e) {
          if (tooltip && tooltip.classList.contains("show")) positionTooltip(e.clientX, e.clientY);
        });
        el.addEventListener("mouseleave", hideTooltip);
        el.addEventListener("click", function () {
          selectedKey = el.getAttribute("data-date");
          showDetail(evs);
          render();
        });
      });
    }

    if (prevBtn) {
      prevBtn.addEventListener("click", function () {
        view = new Date(view.getFullYear(), view.getMonth() - 1, 1);
        render();
      });
    }
    if (nextBtn) {
      nextBtn.addEventListener("click", function () {
        view = new Date(view.getFullYear(), view.getMonth() + 1, 1);
        render();
      });
    }

    render();
  }

  Array.prototype.forEach.call(document.querySelectorAll(".calendar-block"), setup);
})();
