$(function () {
	jQuery("#tbFilter").keypress(function (event) {
		if (event.keyCode == 13) {
			if (jQuery.isFunction(window.__doPostBack))
				__doPostBack('btnFilter', '');
			else {
				location.reload();
			}
			event.stopPropagation();
			return false;
		}
	});
});