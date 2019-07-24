class ModalInteractionDisabler {
    constructor ($modal, disableFilter) {
        this._$modal = $modal;
        this._$disableInside = $modal.find(disableFilter);
    }

    disableInteraction() {
        this._$modal.data('bs.modal')._config.backdrop = 'static';
        this._$modal.data('bs.modal')._config.keyboard = false;
        this._$modal.off('keydown.dismiss.bs.modal');

        this._$disableInside.attr("disabled", "disabled");
    }

    enableInteraction() {
        this._$modal.data('bs.modal')._config.backdrop = true;
        this._$modal.data('bs.modal')._config.keyboard = true;
        this._$modal.data('bs.modal')._setEscapeEvent(); // Force esc key bind

        this._$disableInside.removeAttr("disabled");
    }
}

class BaseActionModal {
    constructor($modal) {
        this._$modal = $modal;
        
        this._$errorContainer = $modal.find('.error-container');
        this._disabler = new ModalInteractionDisabler($modal, '.disable-during-request');
        
        this._bindEvents();
    }

    // Event handlers
    
    _bindEvents() {
        const that = this;

        this._$modal.on('hide.bs.modal', function (e) {
            that._onCancel();
        });

        this._$modal.find('.confirm-button').click(function (e) {
            that._onConfirm(e);
        });
    }

    _onConfirm(e) {
        throw new Error('Needs to be overriden');
    }

    _onCancel() {
        this._postModalCloseCleanup();
    }

    // Closing the modal
    
    _closeModal() {
        this._$modal.modal('hide');
        this._postModalCloseCleanup();
    }

    _postModalCloseCleanup() {
        this._clearError();
    }
    
    // Disabling
    
    _disablePreRequest() {
        this._disabler.disableInteraction();
    }

    _enablePostRequest() {
        this._disabler.enableInteraction();
    }

    // Errors
    
    _clearError() {
        this._$errorContainer.html('');
    }

    _displayError(errorHtml) {
        this._$errorContainer.html(errorHtml);
    }
    
    _displaySimpleError(errorText) {
        this._displayError(`<div class="text-danger">${errorText}</div>`);
    }

    _displayUnknownError() {
        this._displaySimpleError('An unknown error has happened. Please reload the page and try again');
    }
}

class BaseAjaxActionModal extends BaseActionModal{
    _onConfirm() {
        this._ensureActive();

        this._disablePreRequest();
        this._clearError();

        const that = this;
        $.ajax({
            type: 'POST',
            url: this._ajaxTarget,
            data: this._requestData,
            dataType: "json",
            success: function (response) {
                if (response.Success !== undefined) {
                    if (response.Success === true) {
                        that._onAjaxSuccess(response);
                    } else if (response.Success === false && typeof response.HumanErrorHtml === "string") {
                        that._displayError(response.HumanErrorHtml);
                    } else if (response.Success === false && typeof response.HumanError === "string") {
                        that._displaySimpleError(response.HumanError);
                    } else {
                        that._displayUnknownError();
                    }
                } else {
                    console.log(response);
                    that._displayUnknownError();
                }

                that._enablePostRequest();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.log(jqXHR, textStatus, errorThrown);

                that._displayUnknownError();
                that._enablePostRequest();
            }
        });
    }

    // Abstract methods
    
    get isActive() {
        throw new Error('Needs to be overriden');
    }

    get _ajaxTarget() {
        throw new Error('Needs to be overriden');
    }    
    
    get _requestData() {
        throw new Error('Needs to be overriden');
    }
    
    // Methods with default implementation
    
    _onAjaxSuccess (response) {
        this._closeModal();
    }
    
    // Helper methods
    
    _ensureActive() {
        if (!this.isActive) {
            throw new Error('Modal is not active');
        }
    }
    
    _ensureNotActive() {
        if (this.isActive) {
            throw new Error('Modal is already active');
        }
    }
}