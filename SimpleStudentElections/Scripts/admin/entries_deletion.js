class DeletionModal extends BaseAjaxActionModal {
    constructor($modalElement, helpText, submitUrl, onSuccessCallback) {
        super($modalElement);
        
        this._submitUrl = submitUrl;
        this._onSuccessCallback = onSuccessCallback;

        this._$helpText = $modalElement.find('.help-text');
        this._$entriesList = $modalElement.find('.entries-list');

        // We start hidden
        $modalElement.modal('hide');
        this._currentEntries = null;

        // With the specified help text
        this._$helpText.html(helpText);
    }

    askForConfirmation(entries) {
        this._ensureNotActive();

        this._currentEntries = entries;
        this._$entriesList.html(''); // Remove all existing children

        for (let i = 0; i < entries.length; i++) {
            this._$entriesList.append('<li>' + entries[i].displayText + '</li>');
        }

        this._$modal.modal('show');
    }

    get isActive() {
        return this._currentEntries != null;
    }

    get _ajaxTarget() {
        return this._submitUrl;
    }
    
    get _requestData() {
        return {
            'Entries': this._currentEntries.map(entry => entry.data)
        };
    }
    
    _onAjaxSuccess (response) {
        this._onSuccessCallback(response);
        this._closeModal();
    }
    
    _postModalCloseCleanup(){
        super._postModalCloseCleanup();
        this._currentEntries = null;
    }
}

class DeletionModalEntry {
    constructor(displayText, data) {
        this.displayText = displayText;
        this.data = data; // Object with data that will be sent to server which identifies this entry
    }
}