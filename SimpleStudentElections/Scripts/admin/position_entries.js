class PositionEntry extends DeletionModalEntry {
    constructor(positionId, positionName, studentName, userId) {
        const displayName = studentName !== null ? studentName : userId;
        const displayText = `${positionName} - ${displayName}`;

        super(
            displayText,
            {"Username": userId, "PositionId": positionId}
        );
    }
}

class BasePositionEntryModal extends BaseAjaxActionModal {
    constructor($modal) {
        super($modal);

        this._$canNominateYes = $modal.find('.can-nominate-yes');
        this._$canNominateNo = $modal.find('.can-nominate-no');

        this._$canVoteYes = $modal.find('.can-vote-yes');
        this._$canVoteNo = $modal.find('.can-vote-no');
    }

    static _getYesNoValue($yesCheckbox, $noCheckBox) {
        const isYes = $yesCheckbox.is(':checked');
        const isNo = $noCheckBox.is(':checked');

        if (isYes === isNo) {
            throw new Error('Both yes and no checkboxes have the same value');
        }

        return isYes;
    }
}

class PositionEntryEditModal extends BasePositionEntryModal {
    constructor($modal, submitUrl) {
        super($modal);

        this._submitUrl = submitUrl;

        this._$positionName = $modal.find('.position-name-input');
        this._$studentName = $modal.find('.student-name-input');

        this._dtRow = null;
    }

    editEntry(dtRow) {
        if (this.isActive) {
            throw new Error('Modal is already open');
        }

        this._dtRow = dtRow;

        this._populateData();
        this._$modal.modal('show');
    }

    _populateData() {
        const data = this._dtRow.data();

        let studentName = data.UserFullName;
        if (studentName == null) studentName = data.UserId;

        this._$positionName.val(data.PositionName);
        this._$studentName.val(studentName);

        this._$canNominateYes.prop("checked", data.CanNominate === true);
        this._$canNominateNo.prop("checked", data.CanNominate === false);

        this._$canVoteYes.prop("checked", data.CanVote === true);
        this._$canVoteNo.prop("checked", data.CanVote === false);
    }

    get isActive() {
        return this._dtRow != null;
    }

    get _ajaxTarget() {
        return this._submitUrl;
    }

    get _requestData() {
        const data = this._dtRow.data();

        return {
            'UserId': data.UserId,
            'PositionId': data.PositionId,

            'CanNominate': BasePositionEntryModal._getYesNoValue(this._$canNominateYes, this._$canNominateNo),
            'CanVote': BasePositionEntryModal._getYesNoValue(this._$canVoteYes, this._$canVoteNo),
        };
    }

    _onAjaxSuccess(response) {
        // _closeModal() will clear _dtRow so this must be done first 
        this._dtRow.data(response.Entry).draw();

        this._closeModal();
    }

    _postModalCloseCleanup() {
        super._postModalCloseCleanup();
        this._dtRow = null;
    }
}

class NewPositionEntryModal extends BasePositionEntryModal {
    constructor($modal, electionId, $table, submitUrl) {
        super($modal);

        this._electionId = electionId;
        this._$table = $table;
        this._submitUrl = submitUrl;

        this._$positionId = $modal.find('.position-select');
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

            'PositionId': this._$positionId.val(),
            'UserId': this._$studentId.val(),

            'CanNominate': BasePositionEntryModal._getYesNoValue(this._$canNominateYes, this._$canNominateNo),
            'CanVote': BasePositionEntryModal._getYesNoValue(this._$canVoteYes, this._$canVoteNo),
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

        this._$canNominateYes.prop("checked", true);
        this._$canNominateNo.prop("checked", false);

        this._$canVoteYes.prop("checked", true);
        this._$canVoteNo.prop("checked", false);
    }
}