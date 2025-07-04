document.addEventListener('DOMContentLoaded', function () {
  var styleSel// = "DA0481"
  var sizeSel// = "XL"
  var strDateSel = "2023-01-07T00:10:38"
  var endDateSel = "2023-06-08T10:47:57"
  var modulesSel = "All"
  var recData
  var loadingCharts = false;
  const sizeErrorElement = document.getElementById('size-error');

  function toLocalISOString(date) {
    var dateVal = []
    const localDate = new Date(date - date.getTimezoneOffset() * 60000);
    localDate.setMilliseconds(null);
    localDate.setSeconds(null)
    console.log(localDate)
    dateVal[0] = localDate.toISOString().slice(0, -1)
    localDate.setHours(5)
    localDate.setMinutes(30)
    dateVal[1] = localDate.toISOString().slice(0, -1)
    console.log(dateVal)
    return dateVal;
  }

  $('#strDate').prop('value', toLocalISOString(new Date())[1])
  $('#endDate').prop('value', toLocalISOString(new Date())[0])

  function styleLoader(query, syncResults, asyncResults) {
    jQuery.ajax({
      url: 'phpscripts/report/styleLoader.php',
      type: 'POST',
      dataType: 'json',
      success: function (data) {
        console.log('Styles fetched:', data);
        var style = new Bloodhound({
          datumTokenizer: Bloodhound.tokenizers.whitespace,
          queryTokenizer: Bloodhound.tokenizers.whitespace,
          local: data
        });

        $('#style .typeahead').typeahead({
          hint: true,
          highlight: true,
          minLength: 1
        }, {
          source: style
        }).on('typeahead:selected', function (event, selection) {
          loadSizes(selection)
          $('#style-error').prop('hidden', true)
        }).on('blur', function () {
          var inputVal = $(this).typeahead('val');
          style.search(inputVal, function (suggestions) {
            if (!suggestions.length) {
              $('#style-error').prop('hidden', false)
            } else {
              $('#style-error').prop('hidden', true)
              loadSizes(inputVal)
              styleSel = inputVal
            }
          });
        });
      }
    });
  }

  var styles = ['null']
  styleLoader();

  function loadSizes(selection) {
    $('#size .typeahead').typeahead('val', '')
    $('#size .typeahead').typeahead('destroy')
    console.log("Style selected")
    console.log(selection)
    jQuery.ajax({
      url: 'phpscripts/report/sizeLoader.php',
      type: 'POST',
      data: { style: selection },
      dataType: 'json',
      success: function (data) {
        console.log(data)
        var size = new Bloodhound({
          datumTokenizer: Bloodhound.tokenizers.whitespace,
          queryTokenizer: Bloodhound.tokenizers.whitespace,
          local: data
        });

        $('#size .typeahead').prop('disabled', false).typeahead({
          hint: true,
          highlight: true,
          minLength: 1
        }, {
          source: size
        }).on('typeahead:selected', function (event, selection) {
          $('#size-error').prop('hidden', true)
        }).on('blur', function () {
          var inputVal = $(this).typeahead('val');
          size.search(inputVal, function (suggestions) {
            if (!suggestions.length) {
              $('#size-error').prop('hidden', false)
            } else {
              $('#size-error').prop('hidden', true)
              sizeSel = inputVal
              strDateSel = $('input[name=strDate]').val()
              endDateSel = $('input[name=endDate]').val()
              console.log(strDateSel < endDateSel)
              if (strDateSel < endDateSel) {
                const measChart = document.getElementById('chartMea');
                if (measChart) {
                  measChart.destroy()
                }
                if (!loadingCharts) {
                  loadingCharts = true;
                  console.log("Loading charts")
                  loadCharts()
                }
              }
            }
          });
        });
      }
    });
  }

  $('input[name="strDate"]').change(function () {
    strDateSel = $('input[name=strDate]').val()
    endDateSel = $('input[name=endDate]').val()
    console.log(strDateSel)
    if (strDateSel < endDateSel) {
      const measChart = document.getElementById('chartMea');
      // console.log(strDateSel)
      // console.log(endDateSel)
      // console.log(styleSel)
      // console.log(sizeSel)
      // console.log(modulesSel)
                if (measChart) {
                  measChart.destroy()
                }
                if (!loadingCharts) {
                  loadingCharts = true;
                  console.log("Loading charts")
                  loadCharts()
                }
    }
  })

  $('input[name="endDate"]').change(function () {
    strDateSel = $('input[name=strDate]').val()
    endDateSel = $('input[name=endDate]').val()
    // console.log(strDateInp < endDateInp)
    if (strDateSel < endDateSel) {
      const measChart = document.getElementById('chartMea');
                if (measChart) {
                  measChart.destroy()
                }
                if (!loadingCharts) {
                  loadingCharts = true;
                  console.log("Loading charts")
                  loadCharts()
                }
    }
  })

  function loadCharts() {
    var sendData = JSON.stringify({
      style: styleSel,
      size: sizeSel,
      strDate: strDateSel+":00",
      endDate: endDateSel+":00",
      modules: modulesSel
    })
    console.log(sendData)
    fetch('phpscripts/report/chartLoader.php', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: sendData
    })
      .then(response => {
        if (!response.ok) {
          throw new Error('Network response was not ok');
        }
        return response.json();
      })
      .then(data => {
        console.log('Chart Data:', data);
        recData = data
        const measurementsContainer = document.getElementById('measurements');
        measurementsContainer.innerHTML = ''; // Clear previous measurements

        Object.keys(data).forEach(key => {
          if (!isNaN(key)) {
            const measurement = data[key];
            console.log("xss")
            console.log(key)
            console.log(measurement)
            const totalMea = Object.values(measurement.Results).reduce((acc, result) => acc + result.Total, 0);
            const passedMea = Object.values(measurement.Results).reduce((acc, result) => acc + result.Pass, 0);
            const failedMea = totalMea - passedMea;
            const percMea = ((passedMea / totalMea) * 100).toFixed(2);

            // Create a card for each measurement
            const measurementCard = document.createElement('div');
            measurementCard.className = 'col-lg-6 grid-margin stretch-card';
            measurementCard.innerHTML = `
            <div class="card">
              <div class="card-body">
                <h4 class="card-title">${measurement.Measurement}</h4>
                <div class="row d-flex justify-content-center">
                  <div class="card">
                    <div class="card-body">
                      <h6 class="text-primary">Total</h6>
                      <h3 class="display-3">${totalMea}</h3>
                    </div>
                  </div>
                  <div class="card">
                    <div class="card-body">
                      <h6 class="text-primary">Passed</h6>
                      <h3 class="display-3">${passedMea}</h3>
                    </div>
                  </div>
                  <div class="card">
                    <div class="card-body">
                      <h6 class="text-primary">Failed</h6>
                      <h3 class="display-3">${failedMea}</h3>
                    </div>
                  </div>
                  <div class="card">
                    <div class="card-body">
                      <h6 class="text-primary">Percentage</h6>
                      <h3 class="display-3">${percMea}%</h3>
                    </div>
                  </div>
                </div>
              </div>
              <div class="card-body">
                <canvas id="chartMea-${key}" style="height:250px"></canvas>
              </div>
              <div class="card-body">
                <h4 class="card-title">Trend</h4>
                <div class="row d-flex justify-content-center">
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
                        ${Object.keys(measurement.Results).map(resultKey => {
              const result = measurement.Results[resultKey];
              const passPercentage = ((result.Pass / result.Total) * 100).toFixed(2);
              return `
                            <tr>
                              <td>${resultKey}</td>
                              <td>${result.Pass}</td>
                              <td>${result.FailLow || 0}</td>
                              <td>${result.FailHigh || 0}</td>
                              <td>${passPercentage}%</td>
                              <td>${result.Trend}</td>
                            </tr>
                          `;
            }).join('')}
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>
          `;
            measurementsContainer.appendChild(measurementCard);

            // Chart.js setup
            function getTimeUnit(data) {
              const start = new Date(data[0].x).getTime();
              const end = new Date(data[data.length - 1].x).getTime();
              const diff = end - start;

              const oneMinute = 60 * 1000;
              const oneHour = 60 * oneMinute;
              const oneDay = 24 * oneHour;

              if (diff < oneDay) {
                return { unit: 'minute', format: 'yyyy-MM-dd HH:mm:ss' };
              } else if (diff < oneDay * 2) {
                return { unit: 'hour', format: 'yyyy-MM-dd HH:mm:ss' };
              } else {
                return { unit: 'day', format: 'yyyy-MM-dd' };
              }
            }
            var timeUnit
            const ctx = document.getElementById(`chartMea-${key}`).getContext('2d');
            const datasets = Object.keys(measurement.Values).map(valueKey => {
              const value = measurement.Values[valueKey];
              if (value.Label == 'Reference') {
                timeUnit = getTimeUnit(value.data.map(item => ({ x: item[1], y: item[0] })));
              }
              return {
                label: value.Label,
                data: value.data.map(item => ({ x: item[1], y: item[0] })),
                borderColor: value.borderColor || 'rgba(0,0,0,1)',
                fill: value.fill || false
              };
            });

            new Chart(ctx, {
              type: 'line',
              data: {
                datasets: datasets
              },
              options: {
                scales: {
                  x: {
                    type: 'time',
                    time: {
                      unit: timeUnit.unit,
                      tooltipFormat: 'yyyy-MM-DD HH:mm:ss',
                      displayFormats: {
                        day: 'yy-MM-dd HH:mm:ss',
                        hour: 'MM-dd HH:mm:ss',
                        minute: 'MM-dd HH:mm:ss',
                      }
                    },
                    title: {
                      display: true,
                      text: 'Date and Time'
                    }
                  },
                  y: {
                    title: {
                      display: true,
                      text: 'mm'
                    }
                  }
                },
                plugins: {
                  legend: {
                    display: true,
                    position: 'top'
                  }
                }
              }
            });
          }
        });
        loadingCharts = false; // Reset the flag once charts are loaded
      })
      .catch(error => {
        console.error('Error fetching data:', error);
        loadingCharts = false; // Reset the flag in case of error
      });
  }
});
