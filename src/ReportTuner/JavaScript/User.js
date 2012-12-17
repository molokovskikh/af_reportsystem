$(function () {
	$("#tbFilter").keypress(function (event) {
		if (event.keyCode == 13) {
			__doPostBack('btnFilter', '');
			event.stopPropagation();
			return false;
		}
	});
});