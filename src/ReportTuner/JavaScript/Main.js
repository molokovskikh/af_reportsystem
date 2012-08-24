var havePrototype = true;
try {
    Prototype.Version;
}
catch (er) {
    havePrototype = false;
}


if (havePrototype) {
	Event.observe(window, 'load', function() {
        $$('.HighLightCurrentRow').each(function(table) {
            join(table);
        });

        $$('.Paginator').each(function(table) {
            joinPaginator(table);
        });
        
        $$(".ShowHiden").each(function(element) {
            element.onclick = function() { ShowHidden(element); }
        });

        $$(".HideVisible").each(function(element) {
            element.onclick = function() { HideVisible(element); }
        });
    });
}

function join(control) {
	control.select('tr').each(function(row) {
		if (!row.hasClassName("NoHighLightRow")) {
			row.observe('mouseout', function() { row.removeClassName('SelectedRow'); });
			row.observe('mouseover', function() { row.addClassName('SelectedRow'); });
		}
	});
	control.select('li').each(function(row) {
	    row.observe('mouseout',function() { row.removeClassName('SelectedRow'); });
	    row.observe('mouseover', function() { row.addClassName('SelectedRow'); });
	});
}

function joinPaginator(control) {
    control.select('a').each(function(row) {
            row.observe('mouseout', function() { row.removeClassName('Paginator. SelectedRow'); });
            row.observe('mouseover', function() { row.addClassName('Paginator. SelectedRow'); });
    });
}

function processOneParam(param, paramName, paramValue) {
    parts = param.split('=');
    if (parts.length != 2)
        return param;
    if (parts[0] == paramName)
        return paramName + '=' + paramValue;
    else
        return param;
}

function replaceUrlParam(url, paramName, paramValue) {    
    l = url.indexOf('?') + 1;
    if (l == 0)
        return url + '?' + paramName + '=' + paramValue;
    if (url.indexOf('&' + paramName) < 0 && url.indexOf('?' + paramName) < 0)
        return url + '&' + paramName + '=' + paramValue;        
    result = url.substr(0, l);
    query = url.substr(l, url.length - l);
    params = query.split('&');
    result += processOneParam(params[0], paramName, paramValue);
    for (i = 1; i < params.length; i++)
        result += ('&' + processOneParam(params[i], paramName, paramValue));
    return result;    
}

function ReloadPageWithParams(orderParamName, paramValue, rowsCountParamName, rowsCountParamValue) {
    url = window.location.href;
    i = url.lastIndexOf('#');
    if(i>0)
        url = url.substr(0, i);
    url = replaceUrlParam(url, orderParamName, paramValue);
    if(rowsCountParamName && rowsCountParamValue)
        url = replaceUrlParam(url, rowsCountParamName, rowsCountParamValue);
    window.location.href = url;
    return false;
}

function deleteFileForReportType(thisButton) {
	var fileId = jQuery(thisButton).parent().children('input[type=hidden]:first').val();
	jQuery.ajax({
		url: "DeleteFileForReportType?fileId=" + fileId,
		success: function () {
			var linkTd = jQuery(thisButton).parent().parent().children('td.tdForFileLink');
			linkTd.empty();
			linkTd.append('<span class="deletedElement">Удалено</span>');
		}
	});
}