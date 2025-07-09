document.addEventListener('DOMContentLoaded', function () {
  // DOM elements
  const styleInput = document.querySelector('#style .typeahead');
  const sizeInput = document.querySelector('#size .typeahead');
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
  styleInput.addEventListener('change', handleStyleChange);

  function formatDateTimeLocal(date) {
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);
  }

  function toggleDateFilter() {
    startDateInput.disabled = !dateFilterCheckbox.checked;
    endDateInput.disabled = !dateFilterCheckbox.checked;
  }

  function fetchStyles() {
    $.ajax({
      url: '/GM/getStyles',
      method: 'POST',
      success: function (data) {
        const styles = new Bloodhound({
          datumTokenizer: Bloodhound.tokenizers.whitespace,
          queryTokenizer: Bloodhound.tokenizers.whitespace,
          local: data.styles
        });

        $(styleInput).typeahead({
          hint: true,
          highlight: true,
          minLength: 1
        }, {
          source: styles
        }).on('typeahead:select', function (e, style) {
          selectedStyle = style;
          fetchSizes(style);
        });
      },
      error: function (jqXHR, textStatus, errorThrown) {
        console.error('Error loading styles:', textStatus, errorThrown);
      }
    });
  }

  function fetchSizes(style) {
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
          return;
        }

        try { $(sizeInput).typeahead('destroy'); } catch (e) {}

        const sizes = new Bloodhound({
          datumTokenizer: Bloodhound.tokenizers.whitespace,
          queryTokenizer: Bloodhound.tokenizers.whitespace,
          local: sizeList
        });

        $(sizeInput).typeahead({
          hint: true,
          highlight: true,
          minLength: 0
        }, {
          source: sizes
        }).on('typeahead:select', function (e, size) {
          selectedSize = size;
          sizeInput.disabled = false;
        });

        sizeInput.disabled = false;
        sizeInput.value = '';
      },
      error: function (jqXHR, textStatus, errorThrown) {
        console.error('Error loading sizes:', textStatus, errorThrown);
        sizeInput.value = '';
        sizeInput.disabled = true;
      }
    });
  }

  async function handleStyleChange() {
    if (!styleInput.value) {
      sizeInput.value = '';
      sizeInput.disabled = true;
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
      Size: selectedSize,
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

    // Group measurements by type
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
      const card = createMeasurementCard(measurementName, entries);
      measurementsContainer.appendChild(card);
    });
  }

  function createMeasurementCard(measurementName, entries) {
    // Calculate stats
    const total = entries.length;
    const passed = entries.filter(e => e.result === 'Pass').length;
    const failed = total - passed;
    const passPercentage = total > 0 ? ((passed / total) * 100).toFixed(2) : 0;

    // Group by station for table
    const stationsMap = new Map();

    entries.forEach(entry => {
      if (!stationsMap.has(entry.station)) {
        stationsMap.set(entry.station, {
          passed: 0,
          failedLow: 0,
          failedHigh: 0,
          total: 0,
          values: []
        });
      }

      const station = stationsMap.get(entry.station);
      station.total++;
      station.values.push(entry.value);

      if (entry.result === 'Pass') {
        station.passed++;
      } else if (entry.offset < 0) {
        station.failedLow++;
      } else {
        station.failedHigh++;
      }
    });

    // Create card HTML
    const card = document.createElement('div');
    card.className = 'col-lg-6 grid-margin stretch-card';
    card.innerHTML = `
      <div class="card">
        <div class="card-body">
          <h4 class="card-title">${measurementName}</h4>
          <div class="row d-flex justify-content-center">
            <div class="card col-md-3 m-2">
              <div class="card-body">
                <h6 class="text-primary">Total</h6>
                <h3 class="display-3">${total}</h3>
              </div>
            </div>
            <div class="card col-md-3 m-2">
              <div class="card-body">
                <h6 class="text-primary">Passed</h6>
                <h3 class="display-3">${passed}</h3>
              </div>
            </div>
            <div class="card col-md-3 m-2">
              <div class="card-body">
                <h6 class="text-primary">Failed</h6>
                <h3 class="display-3">${failed}</h3>
              </div>
            </div>
            <div class="card col-md-3 m-2">
              <div class="card-body">
                <h6 class="text-primary">Percentage</h6>
                <h3 class="display-3">${passPercentage}%</h3>
              </div>
            </div>
          </div>
        </div>
        <div class="card-body">
          <canvas id="chart-${measurementName.replace(/\s+/g, '-')}"></canvas>
        </div>
        <div class="card-body">
          <h4 class="card-title">Trend</h4>
          <div class="table-responsive">
            <table class="table">
              <thead>
                <tr>
                  <th>Station</th>
                  <th>Passed</th>
                  <th>Failed Low</th>
                  <th>Failed High</th>
                  <th>Pass Percentage</th>
                  <th>Current Trend</th>
                </tr>
              </thead>
              <tbody>
                ${Array.from(stationsMap).map(([station, data]) => {
      const stationPassPercentage = data.total > 0 ?
        ((data.passed / data.total) * 100).toFixed(2) : 0;
      const average = data.values.reduce((a, b) => a + b, 0) / data.values.length;

      return `
                    <tr>
                      <td>${station}</td>
                      <td>${data.passed}</td>
                      <td>${data.failedLow}</td>
                      <td>${data.failedHigh}</td>
                      <td>${stationPassPercentage}%</td>
                      <td>${average.toFixed(2)}</td>
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
      renderChart(measurementName, entries);
    }, 100);

    return card;
  }

  function renderChart(measurementName, entries) {
    const canvas = document.getElementById(`chart-${measurementName.replace(/\s+/g, '-')}`);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const referenceValue = getReferenceValue(measurementName);

    const data = {
      datasets: [
        {
          label: 'Actual Measurements',
          data: entries.map(entry => ({
            x: new Date(entry.dateTime),
            y: entry.value
          })),
          borderColor: 'rgb(75, 192, 192)',
          tension: 0.1
        }
      ]
    };

    if (referenceValue) {
      data.datasets.push({
        label: 'Reference',
        data: entries.map(entry => ({
          x: new Date(entry.dateTime),
          y: referenceValue
        })),
        borderColor: 'rgb(255, 99, 132)',
        borderWidth: 1,
        borderDash: [5, 5],
        pointRadius: 0,
        fill: false
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
            }
          }
        }
      }
    });
  }

  function getReferenceValue(measurementName) {
    // Find reference value from measurement data
    const refMeasurement = referenceData.find(m => m.measurement === measurementName);
    return refMeasurement ? refMeasurement.value : null;
  }

  function handleExport() {
    if (reportData.length === 0) {
      alert('No data to export');
      return;
    }

    let csvContent = 'Garment ID,EPF,SOLI,Station,Date Time,Measurement,Value,Offset,Result\n';

    reportData.forEach(entry => {
      entry.measurements.forEach(measurement => {
        csvContent += `"${entry.garmentId}","${entry.epf}","${entry.soli}","${entry.station}",`;
        csvContent += `"${entry.dateTime}","${measurement.measurement}",`;
        csvContent += `${measurement.value},${measurement.offset},"${measurement.result}"\n`;
      });
    });

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.setAttribute('href', url);
    link.setAttribute('download', `report_${selectedStyle}_${selectedSize}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
});