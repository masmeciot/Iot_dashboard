jQuery(document).ready(function () {

  // function toLocalISOString(date) {
  //   var dateVal = []
  //   const localDate = new Date(date - date.getTimezoneOffset() * 60000); 
  //   localDate.setMilliseconds(null);
  //   localDate.setSeconds(null)
  //   console.log(localDate)
  //   dateVal[0] = localDate.toISOString().slice(0, -1)
  //   localDate.setHours(5)
  //   localDate.setMinutes(30)
  //   dateVal[1] = localDate.toISOString().slice(0, -1)
  //   console.log(dateVal)
  //   return dateVal;
  // }

  // $('#strDate').prop('value',toLocalISOString(new Date())[1])
  // $('#endDate').prop('value',toLocalISOString(new Date())[0])


  // function styleLoader(query, syncResults, asyncResults) {
  //   jQuery.ajax({
  //     url: 'phpscripts/home/styleLoader.php',
  //     type: 'POST',
  //     dataType: 'json',
  //     success: function (data) {
  //       console.log('Styles fetched:', data);
  //       var style = new Bloodhound({
  //         datumTokenizer: Bloodhound.tokenizers.whitespace,
  //         queryTokenizer: Bloodhound.tokenizers.whitespace,
  //         // `states` is an array of state names defined in "The Basics"
  //         local: data
  //       });

  //       $('#style .typeahead').typeahead({
  //         hint: true,
  //         highlight: true,
  //         minLength: 1
  //       }, {
  //         source: style
  //       }).on('typeahead:selected', function (event, selection) {
  //         loadSizes(selection)
  //         $('#style-error').prop('hidden', true)
  //       }).on('blur', function () {
  //         var inputVal = $(this).typeahead('val');
  //         style.search(inputVal, function (suggestions) {
  //           if (!suggestions.length) {
  //             $('#style-error').prop('hidden', false)
  //           } else {
  //             $('#style-error').prop('hidden', true)
  //             loadSizes(inputVal)
  //           }
  //         });
  //       });
  //     }
  //   });
  // }

  // var styles = ['null']
  // styleLoader();
  // function loadSizes(selection) {
  //   $('#size .typeahead').typeahead('val', '')
  //   $('#size .typeahead').typeahead('destroy')
  //   console.log("Style selected")
  //   console.log(selection)
  //   jQuery.ajax({
  //     url: 'phpscripts/home/sizeLoader.php',
  //     type: 'POST',
  //     data: { style: selection },
  //     dataType: 'json',
  //     success: function (data) {
  //       console.log(data)
  //       var size = new Bloodhound({
  //         datumTokenizer: Bloodhound.tokenizers.whitespace,
  //         queryTokenizer: Bloodhound.tokenizers.whitespace,
  //         // `states` is an array of state names defined in "The Basics"
  //         local: data
  //       });

  //       $('#size .typeahead').prop('disabled', false).typeahead({
  //         hint: true,
  //         highlight: true,
  //         minLength: 1
  //       }, {
  //         source: size
  //       }).on('typeahead:selected', function (event, selection) {
  //         $('#size-error').prop('hidden', true)
  //       }).on('blur', function () {
  //         var inputVal = $(this).typeahead('val');
  //         size.search(inputVal, function (suggestions) {
  //           if (!suggestions.length) {
  //             $('#size-error').prop('hidden', false)
  //           } else {
  //             $('#size-error').prop('hidden', true)
  //           }
  //         });
  //       });
  //     }
  //   });
  // };


});
