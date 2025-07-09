document.addEventListener('DOMContentLoaded', function () {
  let liveCharts = {};
  let allCharts = {};

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
        
        // console.debug('About to render LIVE section...');
        // Handle LIVE data
        try {
          renderSection('livedash', data.live?.latest, liveCharts, 'live');
        } catch (e) {
          console.error('Error rendering LIVE section:', e);
        }
        
        // console.debug('About to render ALL section...');
        // Handle ALL data
        try {
          renderSection('alldash', data.live?.all, allCharts, 'all');
        } catch (e) {
          console.error('Error rendering ALL section:', e);
        }
        
        console.debug('LIVE data:', data.live?.latest);
        console.debug('ALL data:', data.live?.all);
      },
      error: function(jqXHR, textStatus, errorThrown) {
        console.error('AJAX error:', textStatus, errorThrown);
        console.error('Status:', jqXHR.status, jqXHR.statusText);
        console.error('Response body:', jqXHR.responseText);
      }
    });
  }

  function renderSection(containerId, data, charts, sectionType) {
    console.debug(`renderSection called for ${sectionType}:`, { containerId, data, sectionType });
    
    const tblContainer = document.getElementById(containerId);
    console.debug(`Container ${containerId} found:`, tblContainer);
    
    if (!tblContainer) {
      console.error(`No #${containerId} container found!`);
      return;
    }
    
    tblContainer.innerHTML = ''; // Clear previous cards
    
    if (!data || !Array.isArray(data)) {
      console.error(`No valid data found for ${sectionType} section! Data:`, data);
      tblContainer.innerHTML = `<div class="col-12 text-center"><p>No data available for ${sectionType.toUpperCase()} section</p></div>`;
      return;
    }
    
    console.debug(`Rendering ${sectionType} cards for`, data.length, 'items');
    
    data.forEach((item, idx) => {
      const total = item.pass + item.fail;
      const pass = item.pass;
      const fail = item.fail;
      const measurements = item.measurements || [];
      
      console.debug(`${sectionType} item ${idx}:`, { item, measurements });

      // Render card
      let cardHtml = `
        <div class="card" data-key="${sectionType}-${idx}" style="margin: 10px; min-height: 70vh; box-sizing: border-box; width: 400px; max-width: 90vw;">
          <div class="card-body" style="display: flex; flex-direction: column; align-items: center; min-height: 70vh; box-sizing: border-box;">
            <div style="font-size: 2.2rem; font-weight: bold; text-align: center;">${item.table}</div>
            <div style="font-size: 1.5rem; font-weight: 600; color: #2196f3; text-align: center; margin-bottom: 8px; letter-spacing: 1px;">${item.style}</div>
            <div style="display: flex; justify-content: center; align-items: center; margin: 10px 0;">
            </div>
            <div class="chart-container" style="height:30vh; max-height:30vh; width: 100%; display: flex; justify-content: center; align-items: center;">
              <canvas id="${sectionType}PassFail-${idx}" style="max-width: 50vh; max-height: 30vh;"></canvas>
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
      console.debug(`Inserted ${sectionType} card for idx`, idx);

      setTimeout(() => {
        const canvas = document.getElementById(`${sectionType}PassFail-${idx}`);
        if (!canvas) {
          console.error(`Canvas not found for ${sectionType} idx`, idx);
          return;
        }
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          console.error(`2D context not found for ${sectionType} canvas idx`, idx);
          return;
        }
        
        const chartId = `${sectionType}PassFail-${idx}`;
        
        // Destroy previous chart if exists
        if (charts[chartId]) {
          charts[chartId].destroy();
        }
        
        try {
          charts[chartId] = new Chart(ctx, {
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
          console.debug(`${sectionType} chart created for idx`, idx);
        } catch (e) {
          console.error(`Chart.js error for ${sectionType} idx`, idx, e);
        }
      }, 0);
    });
  }

  // Initial load
  loadCharts();

  // Refresh charts every 10 seconds
  setInterval(loadCharts, 10000);
});
