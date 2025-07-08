document.addEventListener('DOMContentLoaded', function () {
  let charts = {};

  function loadCharts() {
    console.debug('Calling /GM/getLiveStatus...');
    $.ajax('/GM/getLiveStatus', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      },
      timeout: 20000,
      success: function(data) {
        console.debug('Received data:', data);
        const tblContainer = document.getElementById('livedash');
        if (!tblContainer) {
          console.error('No #livedash container found!');
          return;
        }
        tblContainer.innerHTML = ''; // Clear previous cards
        // Remove cards that are no longer in the data
        const currentCards = tblContainer.querySelectorAll('.card[data-key]');
        currentCards.forEach(card => {
          const key = card.getAttribute('data-key');
          if (!data.live || !data.live.latest || !data.live.latest.hasOwnProperty(key)) {
            card.parentNode.removeChild(card);
          }
        });

        if (!data.live || !data.live.latest) {
          console.error('No data.live.latest found in response!');
          return;
        }
        console.debug('Rendering cards for', data.live.latest.length, 'items');
        data.live.latest.forEach((item, idx) => {
          const total = item.pass + item.fail;
          const pass = item.pass;
          const fail = item.fail;
          const measurements = item.measurements;

          // Render card (simplified)
          let cardHtml = `
            <div class="card" data-key="${idx}" style="margin: 10px; min-height: 70vh; box-sizing: border-box; width: 400px; max-width: 90vw;">
              <div class="card-body" style="display: flex; flex-direction: column; align-items: center; min-height: 70vh; box-sizing: border-box;">
                <div style="font-size: 2.2rem; font-weight: bold; text-align: center;">${item.table}</div>
                <div style="font-size: 1.5rem; font-weight: 600; color: #2196f3; text-align: center; margin-bottom: 8px; letter-spacing: 1px;">${item.style}</div>
                <div style="display: flex; justify-content: center; align-items: center; margin: 10px 0;">
                </div>
                <div class="chart-container" style="height:30vh; max-height:30vh; width: 100%; display: flex; justify-content: center; align-items: center;">
                  <canvas id="passFail-${idx}" style="max-width: 50vh; max-height: 30vh;"></canvas>
                </div>
                <div style="color:#2196f3; font-weight:600; width:100%; text-align:center; margin-top: 10px;">Pass/Total</div>
                <div style="font-size: 2.5rem; font-weight: bold; text-align: center; width:100%;">${pass}/${total}</div>
                <div style="color:#2196f3; font-weight:600; width:100%; text-align:left; margin-top: 10px;">Fail Report</div>
                <div style="width: 100%; margin-top: 6px; text-align: left;">
                  <table style="width: 100%; text-align: left;">
                    <thead>
                      <tr style="font-size: 1rem; color: #555;">
                        <th>Failure</th>
                        <th>Size</th>
                        <th>Qty</th>
                        <th>Trend</th>
                      </tr>
                    </thead>
                    <tbody>
                      ${measurements.map((m, index) => {
                        const isFirstRow = index === 0;
                        const frstrow = isFirstRow ? 'style="color: white; font-weight: bold;"' : '';
                        const rdflshm = m.trend <= -8 ? 'class="flashred"' : '';
                        const orflshm = m.trend <= -6 ? 'class="flashorng"' : '';
                        const ylflshm = m.trend <= -4 ? 'class="flashyllw"' : '';
                        const ylflsh = m.trend >= 4 ? 'class="flashyllw"' : '';
                        const orflsh = m.trend >= 6 ? 'class="flashorng"' : '';
                        const rdflsh = m.trend >= 8 ? 'class="flashred"' : '';
                        const rdUpAr = m.trend >= 4 ? '<i class="mdi mdi-arrow-up text-danger"></i>' : '';
                        const rdDownAr = m.trend <= -4 ? '<i class="mdi mdi-arrow-down text-danger"></i>' : '';
                        const grnUpAr = m.trend > 0 && m.trend < 4 ? '<i class="mdi mdi-arrow-up text-success"></i>' : '';
                        const grnDownAr = m.trend > -4 && m.trend < 0 ? '<i class="mdi mdi-arrow-down text-success"></i>' : '';

                        return `
                          <tr ${frstrow} ${rdflsh} ${orflsh} ${ylflsh} ${rdflshm} ${orflshm} ${ylflshm}>
                            <td>${m.measurement.replace("_", " ")}</td>
                            <td>${m.size}</td>
                            <td>${m.qty}</td>
                            <td>${m.trend} mm ${rdUpAr} ${rdDownAr} ${grnUpAr} ${grnDownAr}</td>
                          </tr>
                        `;
                      }).join('')}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          `;
          tblContainer.innerHTML += cardHtml;
          console.debug('Inserted card for idx', idx);

          setTimeout(() => {
            const canvas = document.getElementById(`passFail-${idx}`);
            if (!canvas) {
              console.error('Canvas not found for idx', idx);
              return;
            }
            const ctx = canvas.getContext('2d');
            if (!ctx) {
              console.error('2D context not found for canvas idx', idx);
              return;
            }
            // Destroy previous chart if exists
            if (charts[`passFail-${idx}`]) {
              charts[`passFail-${idx}`].destroy();
            }
            try {
              charts[`passFail-${idx}`] = new Chart(ctx, {
                type: 'pie',
                data: {
                  datasets: [{
                    data: [pass, fail],
                    backgroundColor: ['rgba(50,205,50,0.8)', 'rgba(200,0,0,0.8)'],
                    borderColor: ['rgb(0,250,0)', 'rgb(250,0,0)'],
                  }],
                  labels: ['Passed', 'Failed']
                },
                options: {
                  responsive: true,
                  maintainAspectRatio: false,
                  animation: {
                    animateScale: true,
                    animateRotate: true
                  }
                }
              });
              console.debug('Chart created for idx', idx);
            } catch (e) {
              console.error('Chart.js error for idx', idx, e);
            }
          }, 0);
        });
      },
      error: function(jqXHR, textStatus, errorThrown) {
        console.error('AJAX error:', textStatus, errorThrown);
        console.error('Status:', jqXHR.status, jqXHR.statusText);
        console.error('Response body:', jqXHR.responseText);
      }
    });
  }

  // Initial load
  loadCharts();

  // Refresh charts every 10 seconds
  setInterval(loadCharts, 10000);
});
