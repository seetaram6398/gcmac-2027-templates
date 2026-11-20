/* Events calendar for 19 · Cascade White (adapted from global.icai.org) */
(function () {
  var events = [
    {
      date: new Date(2026, 11, 15),
      title: "Early Bird registration closes",
      short: "Early Bird closes",
      type: "Early Bird",
      color: "#F1883A",
      desc: "Last date to register at the Early Bird delegate fee — The Ashok, New Delhi.",
      agenda: [
        "All day — Early Bird delegate fee closes",
        "Save ₹2,000 on corporate and ₹1,000 on CMA delegate fees",
        "Register by email: gcmac2027@icmai.in"
      ]
    },
    {
      date: new Date(2027, 1, 12),
      title: "Day 1 · Foundations & Global Perspectives",
      short: "Day 1 · 3 sessions",
      type: "Convention",
      color: "#3D4785",
      desc: "Inaugural plenary, global perspective and the opening fireside chat.",
      agenda: [
        "01:30 PM — Registrations",
        "03:00 PM — Inaugural Session",
        "04:30 PM — Networking Tea",
        "04:45 PM — Plenary: Global CMA Frameworks for Value Governance",
        "06:15 PM — Global Fireside Chat: From Insight to Impact",
        "07:30 PM — Cultural Evening & Networking Dinner"
      ]
    },
    {
      date: new Date(2027, 1, 13),
      title: "Day 2 · Resilience, Energy & Sustainability",
      short: "Day 2 · 5 sessions",
      type: "Convention",
      color: "#1868A8",
      desc: "Concurrent technical sessions, CXO fireside and plenary panel.",
      agenda: [
        "10:00 AM — Concurrent Technical Session I: Ensuring Supply Chain Resilience",
        "01:00 PM — Networking Lunch",
        "02:00 PM — Concurrent Technical Session II: Reconfiguring Energy Sources",
        "05:00 PM — Networking Tea",
        "05:15 PM — Concurrent Technical Session III: Sustainable Development Strategies",
        "02:00 PM — Concurrent Technical Session IV: Global CMA Practices in Service Sector Dynamics",
        "Plenary — CXO Fireside & Panel: Beyond Numbers, Co-Piloting Strategy"
      ]
    },
    {
      date: new Date(2027, 1, 14),
      title: "Day 3 · Digital Transformation & Valedictory",
      short: "Day 3 · 2 sessions",
      type: "Convention",
      color: "#26A951",
      desc: "AI-driven transformation, valedictory session and networking lunch.",
      agenda: [
        "10:00 AM — Technical Session V: Harnessing Capabilities of AI-Driven Digital Transformation",
        "11:30 AM — Networking Tea",
        "11:45 AM — Valedictory Session",
        "01:15 PM — Networking Lunch & Departure"
      ]
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
        if (ev.desc) html += '<div class="cal-tt-desc">' + ev.desc + "</div>";
        if (ev.agenda && ev.agenda.length) {
          var preview = ev.agenda.slice(0, 3);
          html += '<div class="cal-tt-agenda">' + preview.map(function (item) {
            return "<span>" + item + "</span>";
          }).join("") + "</div>";
          if (ev.agenda.length > 3) {
            html += '<div class="cal-tt-more">+' + (ev.agenda.length - 3) + " more — tap the date for the full schedule</div>";
          }
        }
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
        var html = '<strong>' + formatDate(ev.date) + '</strong>';
        html += '<span class="cal-detail-title">' + ev.title + '</span>';
        if (ev.desc) html += '<span class="cal-detail-desc">' + ev.desc + '</span>';
        if (ev.agenda && ev.agenda.length) {
          html += '<ul class="cal-agenda">' + ev.agenda.map(function (item) {
            return '<li>' + item + '</li>';
          }).join('') + '</ul>';
        }
        return html;
      }).join('<hr class="cal-detail-sep">');
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
            html += '<span class="cal-ev-item" style="background:' + ev.color + '" title="' + ev.title + '">' + (ev.short || ev.title) + "</span>";
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

    var now = new Date();
    now.setHours(0, 0, 0, 0);
    var next = events.slice().sort(function (a, b) { return a.date - b.date; }).filter(function (ev) {
      return ev.date.getTime() >= now.getTime();
    })[0];
    if (next) {
      selectedKey = key(next.date);
      showDetail([next]);
      render();
    }
  }

  Array.prototype.forEach.call(document.querySelectorAll(".calendar-block"), setup);
})();
