(function ($) {

	$(function () {
		$.get("/webapi.ashx?view=date").done(function (res) {
			$("#d_main").text(res.value.now);
		});
	});

})(jQuery);