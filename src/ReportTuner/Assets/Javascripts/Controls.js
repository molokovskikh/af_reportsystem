function MarkNotselected() {
	var nsChecked = jQuery(this).attr('checked');
	jQuery('.nsCheckBox').attr('checked', nsChecked);
}

function MarkSelected() {
	var sChecked = jQuery(this).attr('checked');
	jQuery('.sCheckBox').attr('checked', sChecked);
}