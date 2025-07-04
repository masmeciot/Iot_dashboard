document.addEventListener('DOMContentLoaded', function () {
  let charts = {};

  function loadCharts() {
    fetch('phpscripts/home/liveChartLoader.php', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      }
    })
      .then(response => {
        if (!response.ok) {
          throw new Error('Network response was not ok');
        }
        return response.json();
      })
      .then(data => {
        console.log('Chart Data:', data);
        const tblContainer = document.getElementById('livedash');
        
        // Remove cards that are no longer in the data
        const currentCards = tblContainer.querySelectorAll('.card[data-key]');
        currentCards.forEach(card => {
          const key = card.getAttribute('data-key');
          if (!data.hasOwnProperty(key)) {
            card.parentNode.removeChild(card);
          }
        });

        Object.keys(data).forEach(key => {
          if (!isNaN(key)) {
            const tblData = data[key];
            if (tblData.Total == '0') {
              return;
            }

            let tblCard = document.querySelector(`#card-${key}`);
            if (tblCard) {
              // Update existing card
              tblCard.querySelector('.table-title').textContent = tblData.Table;
              tblCard.querySelector('.style-title').textContent = tblData.Style;
              tblCard.querySelector('.pass-total').textContent = `${tblData.Pass}/${tblData.Total}`;

              // Update Fail Breakdown table
              const tbody = tblCard.querySelector('tbody');
              tbody.innerHTML = tblData.FailBreakdown.map((result, index) => {
                const isFirstRow = index === 0;
                const frstrow = isFirstRow ? 'style="color: white; font-weight: bold;"' : '';
                const rdflshm = result[3] <= -8 ? 'class="flashred"' : '';
                const orflshm = result[3] <= -6 ? 'class="flashorng"' : '';
                const ylflshm = result[3] <= -4 ? 'class="flashyllw"' : '';
                const ylflsh = result[3] >= 4 ? 'class="flashyllw"' : '';
                const orflsh = result[3] >= 6 ? 'class="flashorng"' : '';
                const rdflsh = result[3] >= 8 ? 'class="flashred"' : '';
                const rdUpAr = result[3] >= 4 ? '<i class="mdi mdi-arrow-up text-danger">' : '';
                const rdDownAr = result[3] <= -4 ? '<i class="mdi mdi-arrow-down text-danger">' : '';
                const grnUpAr = result[3] > 0 && result[3] < 4 ? '<i class="mdi mdi-arrow-up text-success">' : '';
                const grnDownAr = result[3] > -4 && result[3] < 0 ? '<i class="mdi mdi-arrow-down text-success">' : '';

                return `
                  <tr ${frstrow} ${rdflsh} ${orflsh} ${ylflsh} ${rdflshm} ${orflshm} ${ylflshm}>
                    <td>${result[0].replace("_", " ")}</td> <!-- Fail Measurement -->
                    <td>${result[1]}</td> <!-- Size -->
                    <td>${result[2]}</td> <!-- Fail Quantity -->
                    <td>${result[3]} mm ${rdUpAr} ${rdDownAr} ${grnUpAr} ${grnDownAr}</td> <!-- Trend -->
                  </tr>
                `;
              }).join('');

              // Update Chart.js data
              let chart = charts[key];
              chart.data.datasets[0].data = [tblData.Pass, (tblData.Total - tblData.Pass)];
              chart.update();

            } else {
              // Create a new card for each measurement
              tblCard = document.createElement('div');
              tblCard.id = `card-${key}`;
              tblCard.className = 'col-lg-4 grid-margin stretch-card';
              tblCard.setAttribute('data-key', key);
              tblCard.innerHTML = `
                <div class="card col-4">
                  <div class="card-body" style="padding-left: 10px;padding-right: 10px; padding-top: 10px; padding-bottom: 10px;">
                    <h3 class="display-3 d-flex justify-content-center table-title" style="padding-top: 10px;">${tblData.Table}</h3>
                    <div class="row d-flex justify-content-center">
                      <div class="card col-12">
                        <div class="card-body-alt">
                          <h6 class="text-primary">Style</h6>
                          <h3 class="display-3 d-flex justify-content-center style-title">${tblData.Style}</h3>
                        </div>
                      </div>
                      <div class="card col-12 chart-container" style="padding:5px">
                        <canvas id="passFail-${key}" style="height:content-box"></canvas>
                      </div>
                      <div class="card col-12">
                        <div class="card-body-alt">
                          <h6 class="text-primary">Pass/Total</h6>
                          <h3 class="display-3 d-flex justify-content-center pass-total">${tblData.Pass}/${tblData.Total}</h3>
                        </div>
                      </div>
                      <div class="card col-12">
                        <div class="card-body-alt">
                          <h6 class="text-primary">Fail Report</h6>
                          <div class="row d-flex justify-content-center" style="padding-left:5px; padding-right:5px">
                            <div class="table-responsive">
                              <table class="table">
                                <thead>
                                  <tr>
                                    <th>Failure</th>
                                    <th>Size</th>
                                    <th>Qty</th>
                                    <th>Trend</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  ${tblData.FailBreakdown.map((result, index) => {
                                    const isFirstRow = index === 0;
                                    const frstrow = isFirstRow ? 'style="color: white; font-weight: bold;"' : '';
                                    const rdflshm = result[3] <= -8 ? 'class="flashred"' : '';
                                    const orflshm = result[3] <= -6 ? 'class="flashorng"' : '';
                                    const ylflshm = result[3] <= -4 ? 'class="flashyllw"' : '';
                                    const ylflsh = result[3] >= 4 ? 'class="flashyllw"' : '';
                                    const orflsh = result[3] >= 6 ? 'class="flashorng"' : '';
                                    const rdflsh = result[3] >= 8 ? 'class="flashred"' : '';
                                    const rdUpAr = result[3] >= 4 ? '<i class="mdi mdi-arrow-up text-danger">' : '';
                                    const rdDownAr = result[3] <= -4 ? '<i class="mdi mdi-arrow-down text-danger">' : '';
                                    const grnUpAr = result[3] > 0 && result[3] < 4 ? '<i class="mdi mdi-arrow-up text-success">' : '';
                                    const grnDownAr = result[3] > -4 && result[3] < 0 ? '<i class="mdi mdi-arrow-down text-success">' : '';

                                    return `
                                      <tr ${frstrow} ${rdflsh} ${orflsh} ${ylflsh} ${rdflshm} ${orflshm} ${ylflshm}>
                                        <td>${result[0].replace("_", " ")}</td> <!-- Fail Measurement -->
                                        <td>${result[1]}</td> <!-- Size -->
                                        <td>${result[2]}</td> <!-- Fail Quantity -->
                                        <td>${result[3]} mm ${rdUpAr} ${rdDownAr} ${grnUpAr} ${grnDownAr}</td> <!-- Trend -->
                                      </tr>
                                    `;
                                  }).join('')}
                                </tbody>
                              </table>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>`;
              tblContainer.appendChild(tblCard);

              // Chart.js setup
              const passFail = document.getElementById(`passFail-${key}`).getContext('2d');
              var pieChartData = {
                datasets: [{
                  data: [tblData.Pass, (tblData.Total - tblData.Pass)],
                  backgroundColor: [
                    'rgba(50,205,50,0.8)',
                    'rgba(200, 0, 0, 0.8)'
                  ],
                  borderColor: [
                    'rgb(0, 250, 0)',
                    'rgb(250, 0, 0)'
                  ],
                }],
                // These labels appear in the legend and in the tooltips when hovering different arcs
                labels: [
                  'Passed',
                  'Failed'
                ]
              };

              var pieChartOptions = {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                  animateScale: true,
                  animateRotate: true
                }
              };

              charts[key] = new Chart(passFail, {
                type: 'pie',
                data: pieChartData,
                options: pieChartOptions
              });
            }
          } else {
            console.log("No Keys!");
          }
        });
      })
      .catch(error => {
        console.error('Error fetching data:', error);
      });
  }

  // Initial load
  loadCharts();

  // Refresh charts every 5 seconds
  setInterval(loadCharts, 5000);
});
