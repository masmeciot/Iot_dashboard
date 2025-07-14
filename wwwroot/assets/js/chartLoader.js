document.addEventListener('DOMContentLoaded', function () {
  // DOM elements
  const styleInput = document.querySelector('#style .typeahead');
  const styleDropdown = document.getElementById('style-dropdown');
  const sizeInput = document.querySelector('#size .typeahead');
  const sizeDropdown = document.getElementById('size-dropdown');
  const dateFilterCheckbox = document.getElementById('dateFilterActive');
  const startDateInput = document.getElementById('strDate');
  const endDateInput = document.getElementById('endDate');
  const searchBtn = document.getElementById('searchBtn');
  const exportBtn = document.getElementById('exportBtn');
  const measurementsContainer = document.getElementById('measurements');

  // State variables
  let selectedStyle = '';
  let selectedSize = '';
  let reportData = [];
  let referenceData = {};

  // Initialize date inputs with default values (last 7 days)
  const now = new Date();
  const sevenDaysAgo = new Date();
  sevenDaysAgo.setDate(now.getDate() - 7);

  startDateInput.value = formatDateTimeLocal(sevenDaysAgo);
  endDateInput.value = formatDateTimeLocal(now);

  // Fetch styles on load
  fetchStyles();

  // Event listeners
  searchBtn.addEventListener('click', handleSearch);
  exportBtn.addEventListener('click', handleExport);
  dateFilterCheckbox.addEventListener('change', toggleDateFilter);
  // styleInput.addEventListener('change', handleStyleChange); // This is now handled by the new logic
  // sizeInput.addEventListener('change', handleSizeChange); // This is now handled by the new logic

  function formatDateTimeLocal(date) {
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);
  }

  function toggleDateFilter() {
    startDateInput.disabled = !dateFilterCheckbox.checked;
    endDateInput.disabled = !dateFilterCheckbox.checked;
  }

  // Helper: filter and show dropdown
  function showDropdown(input, dropdown, options) {
    const value = input.value.trim().toLowerCase();
    let filtered = options.filter(opt => opt.toLowerCase().includes(value));
    filtered = filtered.slice(0, 5);
    dropdown.innerHTML = '';
    if (filtered.length === 0) {
      dropdown.classList.remove('show');
      return;
    }
    filtered.forEach(opt => {
      const item = document.createElement('a');
      item.className = 'dropdown-item';
      item.textContent = opt;
      item.href = '#';
      item.onclick = (e) => {
        e.preventDefault();
        input.value = opt;
        input.dispatchEvent(new Event('change'));
        dropdown.classList.remove('show');
      };
      dropdown.appendChild(item);
    });
    dropdown.classList.add('show');
  }

  // Hide dropdown on blur
  function setupDropdownBlur(input, dropdown) {
    input.addEventListener('blur', () => {
      setTimeout(() => dropdown.classList.remove('show'), 150);
    });
  }

  // --- Style logic ---
  let styleOptions = [];
  function fetchStyles() {
    $.ajax({
      url: '/GM/getStyles',
      method: 'POST',
      success: function (data) {
        styleInput.disabled = false;
        styleInput.placeholder = "Select a style";
        styleOptions = data.styles || [];
        styleInput.value = '';
        styleDropdown.innerHTML = '';
      },
      error: function (jqXHR, textStatus, errorThrown) {
        console.error('Error loading styles:', textStatus, errorThrown);
      }
    });
  }
  styleInput.addEventListener('input', function () {
    showDropdown(styleInput, styleDropdown, styleOptions);
  });
  styleInput.addEventListener('focus', function () {
    showDropdown(styleInput, styleDropdown, styleOptions);
  });
  styleInput.addEventListener('change', function () {
    selectedStyle = styleInput.value;
    fetchSizes(selectedStyle);
  });
  setupDropdownBlur(styleInput, styleDropdown);

  // --- Size logic ---
  let sizeOptions = [];
  function fetchSizes(style) {
    sizeInput.disabled = true;
    sizeInput.value = '';
    sizeDropdown.innerHTML = '';
    sizeInput.placeholder = "Loading sizes...";
    $.ajax({
      url: '/GM/getAvailSizes',
      method: 'POST',
      data: { style: style },
      success: function (data) {
        let sizeList = [];
        if (Array.isArray(data)) {
          sizeList = data;
        } else if (data && Array.isArray(data.sizes)) {
          sizeList = data.sizes;
        }
        if (!sizeList.length) {
          sizeInput.value = '';
          sizeInput.disabled = true;
          sizeInput.placeholder = "No sizes available";
          sizeOptions = [];
          sizeDropdown.innerHTML = '';
          return;
        }
        sizeInput.disabled = false;
        sizeInput.placeholder = "Select a size";
        // Add "All" option at the beginning
        sizeOptions = ['All', ...sizeList];
        sizeInput.value = '';
        sizeDropdown.innerHTML = '';
      },
      error: function (jqXHR, textStatus, errorThrown) {
        console.error('Error loading sizes:', textStatus, errorThrown);
        sizeInput.value = '';
        sizeInput.disabled = true;
        sizeInput.placeholder = "Error loading sizes";
        sizeOptions = [];
        sizeDropdown.innerHTML = '';
      }
    });
  }
  sizeInput.addEventListener('input', function () {
    showDropdown(sizeInput, sizeDropdown, sizeOptions);
  });
  sizeInput.addEventListener('focus', function () {
    showDropdown(sizeInput, sizeDropdown, sizeOptions);
  });
  sizeInput.addEventListener('change', function () {
    selectedSize = sizeInput.value;
  });
  setupDropdownBlur(sizeInput, sizeDropdown);

  async function handleStyleChange() {
    if (!styleInput.value) {
      sizeInput.value = '';
      sizeInput.disabled = true;
      sizeInput.classList.remove('disabled'); // (optional, for Bootstrap)
      sizeInput.style.backgroundColor = '';
      sizeInput.style.color = '';
      sizeInput.style.cursor = '';
      sizeInput.placeholder = "Select a style 1st"
      selectedSize = '';
    }
  }

  function handleSearch() {
    if (!selectedStyle || !selectedSize) {
      alert('Please select both style and size');
      return;
    }

    let startDate, endDate;
    if (dateFilterCheckbox.checked) {
      startDate = new Date(startDateInput.value).toISOString();
      endDate = new Date(endDateInput.value).toISOString();
    } else {
      const now = new Date().toISOString();
      startDate = now;
      endDate = now;
    }

    const params = {
      Style: selectedStyle,
      Size: selectedSize, // Send the actual selected size value (including "All")
      DateFilter: dateFilterCheckbox.checked ? "Yes" : "No",
      StartDate: startDate,
      EndDate: endDate
    };

    // Fetch report data using $.ajax
    $.ajax({
      url: '/GM/getReport',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(params),
      success: function (reportResult) {
        reportData = reportResult.report || reportResult.Report || [];
        // Fetch reference measurements after report data is loaded
        $.ajax({
          url: '/GM/getMeasurements',
          method: 'POST',
          data: { style: selectedStyle },
          success: function (response) {
            referenceData = response.measurements;
            buildDashboard();
          },
          error: function (jqXHR, textStatus, errorThrown) {
            console.error('Error loading reference measurements:', textStatus, errorThrown);
            alert('Error loading reference measurements.');
          }
        });
      },
      error: function (jqXHR, textStatus, errorThrown) {
        console.error('Error loading report data:', textStatus, errorThrown);
        alert('Error loading report data. Please try again.');
      }
    });
  }

  function buildDashboard() {
    measurementsContainer.innerHTML = '';

    // If "All" is selected, group by size and create separate charts for each size
    if (selectedSize === 'All') {
      // Group report data by size
      const sizeGroups = new Map();

      reportData.forEach(entry => {
        const size = entry.size || 'Unknown';
        if (!sizeGroups.has(size)) {
          sizeGroups.set(size, []);
        }
        sizeGroups.get(size).push(entry);
      });

      // Create measurement cards for each size
      sizeGroups.forEach((sizeData, size) => {
        // Group measurements by type for this size
        const measurementsMap = new Map();

        sizeData.forEach(entry => {
          entry.measurements.forEach(measurement => {
            if (!measurementsMap.has(measurement.measurement)) {
              measurementsMap.set(measurement.measurement, []);
            }
            measurementsMap.get(measurement.measurement).push({
              ...measurement,
              garmentId: entry.garmentId,
              dateTime: entry.dateTime,
              station: entry.station,
              epf: entry.epf,
              soli: entry.soli
            });
          });
        });

        // Create cards for each measurement type in this size
        measurementsMap.forEach((entries, measurementName) => {
          const card = createMeasurementCard(measurementName, entries, size);
          measurementsContainer.appendChild(card);
        });
      });
    } else {
      // Original logic for specific size selection
      const measurementsMap = new Map();

      reportData.forEach(entry => {
        entry.measurements.forEach(measurement => {
          if (!measurementsMap.has(measurement.measurement)) {
            measurementsMap.set(measurement.measurement, []);
          }
          measurementsMap.get(measurement.measurement).push({
            ...measurement,
            garmentId: entry.garmentId,
            dateTime: entry.dateTime,
            station: entry.station,
            epf: entry.epf,
            soli: entry.soli
          });
        });
      });

      // Create cards for each measurement type
      measurementsMap.forEach((entries, measurementName) => {
        const card = createMeasurementCard(measurementName, entries, selectedSize);
        measurementsContainer.appendChild(card);
      });
    }
  }

  function createMeasurementCard(measurementName, entries, size) {
    // Calculate stats
    const total = entries.length;
    const passed = entries.filter(e => e.result === 'Pass').length;
    const failed = total - passed;
    const passPercentage = total > 0 ? ((passed / total) * 100).toFixed(2) : 0;
    const refValues = getReferenceValues(measurementName, size);

    // Group by station for table
    const stationsMap = new Map();

    entries.forEach(entry => {
      if (!stationsMap.has(entry.station)) {
        stationsMap.set(entry.station, {
          passed: 0,
          failedLow: 0,
          failedHigh: 0,
          total: 0,
          values: [],
          offsets: [],
          timestamps: []
        });
      }

      const station = stationsMap.get(entry.station);
      station.total++;
      station.values.push(entry.value);
      station.offsets.push(entry.offset);
      station.timestamps.push(new Date(entry.dateTime).getTime());

      if (entry.result === 'Pass') {
        station.passed++;
      } else if (entry.offset < 0) {
        station.failedLow++;
      } else {
        station.failedHigh++;
      }
    });

    // Calculate linear regression trend for each station (per garment)
    function calculateTrend(offsets) {
      if (offsets.length < 2) return { slope: 0, trend: 'No trend' };
      
      const n = offsets.length;
      const xs = Array.from({length: n}, (_, i) => i); // Garment indices
      const sumX = xs.reduce((a, b) => a + b, 0);
      const sumY = offsets.reduce((a, b) => a + b, 0);
      const sumXY = xs.reduce((sum, x, i) => sum + x * offsets[i], 0);
      const sumX2 = xs.reduce((sum, x) => sum + x * x, 0);
      
      const slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
      const intercept = (sumY - slope * sumX) / n;
      
      let trend;
      if (Math.abs(slope) < 0.01) {
        trend = 'Stable';
      } else if (slope > 0) {
        trend = `↗ +${slope.toFixed(3)}/garment`;
      } else {
        trend = `↘ ${slope.toFixed(3)}/garment`;
      }
      
      return { slope, trend };
    }

    // Create card HTML
    const card = document.createElement('div');
    card.className = 'col-lg-6 grid-margin stretch-card';
    card.innerHTML = `
      <div class="card measurement-card">
        <div class="card-body">
          <h4 class="card-title measurement-title">${measurementName}</h4>
          <div class="row d-flex justify-content-center card-stats-row single-row-stats">
            <div class="col stat-col">
              <div class="stat-label">Size</div>
              <div class="stat-value"><strong>${size}</strong></div>
            </div>
            <div class="col stat-col">
              <div class="stat-label">Total</div>
              <div class="stat-value"><strong>${total}</strong></div>
            </div>
            <div class="col stat-col">
              <div class="stat-label">Passed</div>
              <div class="stat-value"><strong>${passed}</strong></div>
            </div>
            <div class="col stat-col">
              <div class="stat-label">Failed</div>
              <div class="stat-value"><strong>${failed}</strong></div>
            </div>
            <div class="col stat-col">
              <div class="stat-label">Percentage</div>
              <div class="stat-value"><strong>${passPercentage}%</strong></div>
            </div>
          </div>
        </div>
        <div class="card-body card-chart-body">
          <canvas id="chart-${measurementName.replace(/\s+/g, '-')}-${size.replace(/\s+/g, '-')}" class="measurement-chart"></canvas>
        </div>
        <div class="card-body">
          <h4 class="card-title measurement-title">Trend</h4>
          <div class="table-responsive measurement-table-scroll" style="max-height: 180px; overflow-y: auto;">
            <table class="table table-sm">
              <thead>
                <tr>
                  <th>Station</th>
                  <th>Passed</th>
                  <th>Failed Low</th>
                  <th>Failed High</th>
                  <th>Pass %</th>
                  <th>Current Trend</th>
                </tr>
              </thead>
              <tbody>
                ${Array.from(stationsMap).map(([station, data]) => {
      const stationPassPercentage = data.total > 0 ?
        ((data.passed / data.total) * 100).toFixed(2) : 0;
      const trend = calculateTrend(data.offsets);

      return `
                    <tr>
                      <td>${station}</td>
                      <td>${data.passed}</td>
                      <td>${data.failedLow}</td>
                      <td>${data.failedHigh}</td>
                      <td>${stationPassPercentage}%</td>
                      <td>${trend.trend}</td>
                    </tr>
                  `;
    }).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    // Render chart after DOM insertion
    setTimeout(() => {
      renderChart(measurementName, entries, size);
    }, 100);

    return card;
  }

  function renderChart(measurementName, entries, size) {
    const canvas = document.getElementById(`chart-${measurementName.replace(/\s+/g, '-')}-${size.replace(/\s+/g, '-')}`);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    // Get reference values for selected size
    const refValues = getReferenceValues(measurementName, size);

    // In renderChart, use exponential moving average (EMA) for the trend line
    function calculateEMATrendLine(entries, alpha = 0.2) {
      if (entries.length < 2) return null;
      const emaData = [];
      let prevEMA = entries[0].value;
      entries.forEach((entry, i) => {
        const ema = i === 0 ? entry.value : alpha * entry.value + (1 - alpha) * prevEMA;
        emaData.push({ x: new Date(entry.dateTime), y: ema });
        prevEMA = ema;
      });
      return emaData;
    }

    const emaTrendLine = calculateEMATrendLine(entries);

    const data = {
      datasets: [
        {
          label: 'Actual Measurements',
          data: entries.map(entry => ({
            x: new Date(entry.dateTime),
            y: entry.value
          })),
          borderColor: 'rgb(75, 192, 192)',
          backgroundColor: 'rgba(75, 192, 192, 0.1)',
          tension: 0.1,
          pointRadius: 3
        }
      ]
    };

    // Add EMA trend line if available
    if (emaTrendLine && emaTrendLine.length > 1) {
      data.datasets.push({
        label: 'EMA Trend',
        data: emaTrendLine,
        borderColor: 'rgb(255, 165, 0)',
        borderWidth: 2,
        borderDash: [8, 4],
        pointRadius: 0,
        fill: false
      });
    }

    if (refValues) {
      // Add reference lines
      data.datasets.push({
        label: 'Target',
        data: entries.map(entry => ({
          x: new Date(entry.dateTime),
          y: refValues.reference
        })),
        borderColor: 'rgb(255, 99, 132)',
        borderWidth: 2,
        pointRadius: 0,
        fill: false
      });

      data.datasets.push({
        label: 'Max Limit',
        data: entries.map(entry => ({
          x: new Date(entry.dateTime),
          y: refValues.reference + refValues.tolerance
        })),
        borderColor: 'rgb(0, 128, 0)',
        borderWidth: 1,
        borderDash: [5, 5],
        pointRadius: 0,
        fill: false
      });

      data.datasets.push({
        label: 'Min Limit',
        data: entries.map(entry => ({
          x: new Date(entry.dateTime),
          y: refValues.reference - refValues.tolerance
        })),
        borderColor: 'rgb(0, 128, 0)',
        borderWidth: 1,
        borderDash: [5, 5],
        pointRadius: 0,
        fill: false
      });

      // Add fill between min and max
      data.datasets.push({
        label: 'Acceptable Range',
        data: [
          ...entries.map(entry => ({
            x: new Date(entry.dateTime),
            y: refValues.reference - refValues.tolerance
          })),
          ...entries.map(entry => ({
            x: new Date(entry.dateTime),
            y: refValues.reference + refValues.tolerance
          })).reverse()
        ],
        backgroundColor: 'rgba(0, 128, 0, 0.1)',
        borderWidth: 0,
        pointRadius: 0,
        fill: true
      });
    }

    new Chart(ctx, {
      type: 'line',
      data: data,
      options: {
        scales: {
          x: {
            type: 'time',
            time: {
              unit: 'day',
              tooltipFormat: 'yyyy-MM-dd HH:mm'
            },
            title: {
              display: true,
              text: 'Date and Time'
            }
          },
          y: {
            title: {
              display: true,
              text: 'Measurement Value (mm)'
            },
            suggestedMin: refValues ? refValues.reference - refValues.tolerance * 2 : undefined,
            suggestedMax: refValues ? refValues.reference + refValues.tolerance * 2 : undefined
          }
        },
        plugins: {
          tooltip: {
            callbacks: {
              label: function (context) {
                const label = context.dataset.label || '';
                if (label === 'Actual Measurements') {
                  const entry = entries[context.dataIndex];
                  return `${label}: ${context.parsed.y}mm (Offset: ${entry.offset}mm)`;
                } else if (label === 'EMA Trend') {
                  return `${label}: ${context.parsed.y}mm`;
                }
                return `${label}: ${context.parsed.y}mm`;
              }
            }
          }
        }
      }
    });
  }

  function getReferenceValues(measurementName, size) {
    // Find reference value for the specific size
    const sizeRef = referenceData.find(s => s.size === size);
    if (!sizeRef) return null;

    const refMeasurement = sizeRef.measurements.find(m =>
      m.measurement === measurementName
    );

    return refMeasurement ? {
      reference: refMeasurement.reference,
      tolerance: refMeasurement.tolerance
    } : null;
  }

  function handleExport() {
    if (reportData.length === 0) {
      alert('No data to export');
      return;
    }

    // Get all unique measurement names for column headers
    const measurementNames = new Set();
    reportData.forEach(entry => {
      entry.measurements.forEach(measurement => {
        measurementNames.add(measurement.measurement);
      });
    });
    const sortedMeasurementNames = Array.from(measurementNames).sort();

    // Create CSV header
    let csvContent = 'Garment ID,Station,EPF,Session,DateTime,Size,SOLI,Overall Status';

    // Add measurement columns (3 columns per measurement: value, offset, status)
    sortedMeasurementNames.forEach(measurementName => {
      csvContent += `,${measurementName},${measurementName} Offset,${measurementName} Status`;
    });
    csvContent += '\n';

    // Group data by garment ID
    const garmentGroups = new Map();
    reportData.forEach(entry => {
      if (!garmentGroups.has(entry.garmentId)) {
        garmentGroups.set(entry.garmentId, {
          garmentId: entry.garmentId,
          station: entry.station,
          epf: entry.epf,
          session: entry.session || '',
          dateTime: entry.dateTime,
          size: entry.size || selectedSize,
          soli: entry.soli,
          measurements: new Map()
        });
      }

      const garment = garmentGroups.get(entry.garmentId);
      entry.measurements.forEach(measurement => {
        garment.measurements.set(measurement.measurement, {
          value: measurement.value,
          offset: measurement.offset,
          result: measurement.result
        });
      });
    });

    // Calculate overall status for each garment
    garmentGroups.forEach(garment => {
      const allMeasurements = Array.from(garment.measurements.values());
      const allPassed = allMeasurements.every(m => m.result === 'Pass');
      garment.overallStatus = allPassed ? 'Pass' : 'Fail';
    });

    // Create CSV rows
    garmentGroups.forEach(garment => {
      // Basic garment info
      csvContent += `"${garment.garmentId}","${garment.station}","${garment.epf}","${garment.session}","${garment.dateTime}","${garment.size}","${garment.soli}","${garment.overallStatus}"`;

      // Add measurement data for each measurement type
      sortedMeasurementNames.forEach(measurementName => {
        const measurement = garment.measurements.get(measurementName);
        if (measurement) {
          csvContent += `,"${measurement.value}","${measurement.offset}","${measurement.result}"`;
        } else {
          csvContent += `,"","",""`; // Empty values if measurement not found
        }
      });

      csvContent += '\n';
    });

    // Create and download the file
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.setAttribute('href', url);
    link.setAttribute('download', `report_${selectedStyle}_${selectedSize}_${new Date().toISOString().slice(0, 10)}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Clean up the URL object
    URL.revokeObjectURL(url);
  }
});

// Remove any previously injected style blocks for measurement-card to avoid duplicates
const prevStyle = document.getElementById('measurement-card-style');
if (prevStyle) prevStyle.remove();
const style = document.createElement('style');
style.id = 'measurement-card-style';
style.innerHTML = `
.measurement-card {
  margin-bottom: 18px;
  font-size: 0.97rem;
}
.measurement-title {
  font-size: 1.08rem !important;
  font-weight: 600;
  margin-bottom: 8px;
}
.card-stats-row.single-row-stats {
  display: flex;
  flex-direction: row;
  justify-content: center;
  align-items: stretch;
  margin-bottom: 8px !important;
  gap: 0;
}
.stat-col {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  height: 100%;
}
.stat-label {
  font-size: 1.05rem !important;
  margin-bottom: 2px !important;
  color: #2196f3;
  font-weight: 600;
  text-align: center;
  width: 100%;
}
.stat-value {
  font-size: 2.1rem !important;
  font-weight: 900 !important;
  margin-bottom: 0 !important;
  color: #fff;
  text-align: center;
  width: 100%;
  line-height: 1.1;
}
.card-chart-body {
  margin-bottom: 8px !important;
  padding: 8px 0 0 0 !important;
}
.measurement-chart {
  height: 30vh !important;
  max-height: 30vh !important;
  padding-left: 1.5%;
  padding-right: 1.5%;
}
.measurement-table-scroll {
  max-height: 180px !important;
  overflow-y: auto !important;
}
.table-sm th, .table-sm td {
  padding: 0.35rem 0.5rem !important;
  font-size: 0.93rem !important;
}
`;
document.head.appendChild(style);