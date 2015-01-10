(function ($) {

	$(function () {
		// get the current value of data.txt
		$.get("/webapi.ashx?view=db").done(function (res) {
			$("#d_main").text(res.value.Text);
		});

		// save the input value to data.txt
		$("button").click(function (ev) {
			$.ajax({ method:"POST", url: "/webapi.ashx?view=db&action=save", data: JSON.stringify($("#v_data").val()) }).done(function (res) {
				alert(res.value);
			})
		});
	});

})(jQuery);