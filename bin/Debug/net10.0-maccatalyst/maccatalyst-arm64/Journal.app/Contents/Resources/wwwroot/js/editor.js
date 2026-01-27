window.editor = {
    insertTextAtCursor: function (elementId, textBefore, textAfter) {
        var el = document.getElementById(elementId);
        if (!el) return;

        var start = el.selectionStart;
        var end = el.selectionEnd;
        var text = el.value;
        var selectedText = text.substring(start, end);

        var newText = textBefore + selectedText + (textAfter || '');
        el.value = text.substring(0, start) + newText + text.substring(end);

        // Restore focus and selection
        el.focus();
        el.selectionStart = start + textBefore.length;
        el.selectionEnd = end + textBefore.length;

        // Trigger input event to update Blazor binding
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
    }
};
