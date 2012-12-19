$(function () {
	$("#tbFilter").keypress(function (event) {
		if (event.keyCode == 13) {
			if ($.isFunction(window.__doPostBack))
				__doPostBack('btnFilter', '');
			else {
				location.reload();
			}
			event.stopPropagation();
			return false;
		}
	});
});