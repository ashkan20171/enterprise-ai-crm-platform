(() => {
  'use strict';
  const chart = document.getElementById('crm92-pipeline');
  const controls = document.querySelectorAll('[data-crm92-metric]');
  if (!chart || !controls.length) return;
  function setMetric(metric) {
    if (metric !== 'value' && metric !== 'count') return;
    chart.querySelectorAll('.crm56-fill[data-crm92-value-width]').forEach(bar => {
      const raw = metric === 'count' ? bar.dataset.crm92CountWidth : bar.dataset.crm92ValueWidth;
      const width = Number(raw);
      bar.style.width = `${Number.isFinite(width) ? Math.max(0, Math.min(100, width)) : 0}%`;
    });
    chart.querySelectorAll('[data-crm92-value]').forEach(label => {
      label.textContent = metric === 'count' ? label.dataset.crm92Count : label.dataset.crm92Value;
    });
    controls.forEach(button => {
      const selected = button.dataset.crm92Metric === metric;
      button.classList.toggle('is-selected', selected);
      button.setAttribute('aria-pressed', String(selected));
    });
  }
  controls.forEach(button => button.addEventListener('click', () => setMetric(button.dataset.crm92Metric)));
})();
