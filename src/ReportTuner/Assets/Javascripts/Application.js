
function SetCookie(name, value, options) {
	if (value == null) {
		this.setCookie(name, "", { expires: -1 });
		return;
	}
	options = options || {};
	if (!options.path)
		options.path = '/';

	var expires = options.expires;

	if (typeof expires == "number" && expires) {
		var d = new Date();
		d.setTime(d.getTime() + expires * 1000);
		expires = options.expires = d;
	}
	if (expires && expires.toUTCString) {
		options.expires = expires.toUTCString();
	}

	var updatedCookie = name + "=" + value;

	for (var propName in options) {
		updatedCookie += "; " + propName;
		var propValue = options[propName];
		if (propValue !== true) {
			updatedCookie += "=" + propValue;
		}
	}
	document.cookie = updatedCookie;
}

function GetCookie(name, eraseFlag) {
	var matches = document.cookie.match(new RegExp(
		"(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
	));
	var ret = matches ? (matches[1]) : undefined;
	if (eraseFlag)
		this.setCookie(name, null);
	return ret;
}

function MarkNotselected() {
	var nsChecked = $(this).attr('checked');
	$('.nsCheckBox').attr('checked', nsChecked);
}

function MarkSelected() {
	var sChecked = $(this).attr('checked');
	$('.sCheckBox').attr('checked', sChecked);
}

function join(control) {
	$(control).find('tr').each(function () {
		if (!$(this).hasClassName("NoHighLightRow")) {
			$(this).bind('mouseout', function () { $(this).removeClass('SelectedRow'); });
			$(this).bind('mouseover', function () { $(this).addClass('SelectedRow'); });
		}
	});
	$(control).find('li').each(function () {
		$(this).bind('mouseout', function () { $(this).removeClass('SelectedRow'); });
		$(this).bind('mouseover', function () { $(this).addClass('SelectedRow'); });
	});
}

function joinPaginator(control) {
	$(control).find('a').each(function () {
		$(this).bind('mouseout', function () { $(this).removeClass('Paginator. SelectedRow'); });
		$(this).bind('mouseover', function () { $(this).addClass('Paginator. SelectedRow'); });
	});
}

function processOneParam(param, paramName, paramValue) {
	var parts = param.split('=');
	if (parts.length != 2)
		return param;
	if (parts[0] == paramName)
		return paramName + '=' + paramValue;
	else
		return param;
}

function replaceUrlParam(url, paramName, paramValue) {
	var l = url.indexOf('?') + 1;
	if (l == 0)
		return url + '?' + paramName + '=' + paramValue;
	if (url.indexOf('&' + paramName) < 0 && url.indexOf('?' + paramName) < 0)
		return url + '&' + paramName + '=' + paramValue;
	var result = url.substr(0, l);
	var query = url.substr(l, url.length - l);
	var params = query.split('&');
	result += processOneParam(params[0], paramName, paramValue);
	for (var i = 1; i < params.length; i++)
		result += ('&' + processOneParam(params[i], paramName, paramValue));
	return result;
}

function ReloadPageWithParams(orderParamName, paramValue, rowsCountParamName, rowsCountParamValue) {
	var url = window.location.href;
	var i = url.lastIndexOf('#');
	if (i > 0)
		url = url.substr(0, i);
	url = replaceUrlParam(url, orderParamName, paramValue);
	if (rowsCountParamName && rowsCountParamValue)
		url = replaceUrlParam(url, rowsCountParamName, rowsCountParamValue);
	window.location.href = url;
	return false;
}

function deleteFileForReportType(thisButton) {
	var fileId = jQuery(thisButton).parent().children('input[type=hidden]:first').val();
	$.ajax({
		url: "DeleteFileForReportType?fileId=" + fileId,
		success: function () {
			var linkTd = jQuery(thisButton).parent().parent().children('td.tdForFileLink');
			linkTd.empty();
			linkTd.append('<span class="deletedElement">Удалено</span>');
		}
	});
}

$(function () {
	$("#tbFilter").keypress(function (event) {
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

	$('.HighLightCurrentRow').each(function (table) {
		join(table);
	});

	$('.Paginator').each(function (table) {
		joinPaginator(table);
	});
});
