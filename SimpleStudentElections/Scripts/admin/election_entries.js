class ElectionEntry extends DeletionModalEntry {
    constructor(studentName, userId) {
        const displayName = studentName !== null ? studentName : userId;

        super(
            displayName,
            {"Username": userId}
        );
    }
}

class NewElectionEntryModal extends BaseAjaxActionModal {
    constructor($modal, electionId, $table, submitUrl) {
        super($modal);

        this._electionId = electionId;
        this._$table = $table;
        this._submitUrl = submitUrl;

        this._$studentId = $modal.find('.student-id-input');

        this._isActive = false;
    }

    open() {
        if (this.isActive) {
            throw new Error('Modal is already open');
        }

        this._revertFieldsToDefault();

        this._isActive = true;
        this._$modal.modal('show');
    }

    get isActive() {
        return this._isActive;
    }

    get _ajaxTarget() {
        return this._submitUrl;
    }

    get _requestData() {
        return {
            // While technically redundant, this is here to make sure we are working with the correct election 
            'ElectionId': this._electionId,
            'UserId': this._$studentId.val(),
        };
    }

    _onAjaxSuccess(response) {
        this._$table.DataTable()
            .row.add(response.Entry)
            .draw(false); // false prevents paging reset

        this._closeModal();
    }

    _postModalCloseCleanup() {
        super._postModalCloseCleanup();
        this._isActive = false;
    }

    _revertFieldsToDefault() {
        this._$studentId.val('');
    }
}