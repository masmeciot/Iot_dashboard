const ctx = document.getElementById('lineChart').getContext('2d');

// Sample Data
const dataset1 = {
  label: 'Dataset 1',
  data: [
    { x: '2024-05-21T10:00:00', y: 10 },
    { x: '2024-05-22T11:00:00', y: 5 },
    { x: '2024-05-23T12:00:00', y: 20 },
    { x: '2024-05-24T12:00:00', y: 70 },
    { x: '2024-05-25T11:00:00', y: 5 },
    { x: '2024-05-26T12:00:00', y: 20 },
    { x: '2024-05-27T12:00:00', y: 70 }
  ],
  borderColor: 'red',
  fill: false
};

const dataset2 = {
  label: 'Dataset 2',
  data: [
    { x: '2024-05-15T10:30:00', y: 5 },
    { x: '2024-05-19T11:30:00', y: 87 },
    { x: '2024-05-24T12:30:00', y: 48 }
  ],
  borderColor: 'blue',
  fill: false
};

const dataset3 = {
  label: 'Dataset 3',
  data: [
    { x: '2024-05-2T09:45:00', y: 2 },
    { x: '2024-05-12T10:45:00', y: 66 },
    { x: '2024-05-16T11:45:00', y: 6 },
    { x: '2024-05-17T09:45:00', y: 2 },
    { x: '2024-05-25T10:45:00', y: 35 }
  ],
  borderColor: 'green',
  fill: false
};
// data: {
//   datasets: [dataset1, dataset2, dataset3]
// },
// Chart Configuration
const config = {
  type: 'line',
  data: {
    datasets: [dataset1, dataset2, dataset3]
  },
  options: {
    scales: {
      x: {
        type: 'time',
        time: {
          unit: 'day',
          tooltipFormat: 'yyyy-MM-DD HH:mm',
          displayFormats: {
            day: 'yy-MM-dd HH:mm'
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
          text: 'Count'
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
};

// Create the Chart
const myChart = new Chart(ctx, config);
