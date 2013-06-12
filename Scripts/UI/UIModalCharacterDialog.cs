using UnityEngine;
using System.Collections;

[AddComponentMenu("M8/UI/ModalCharacterDialog")]
public class UIModalCharacterDialog : UIController {
    //index = -1 if there are no choices and we clicked next
    public delegate void OnAction(int choiceIndex);

    public const string defaultModalRef = "CharacterDialog";

    public UISprite portrait; //optional
    public UILabel nameLabel; //optional
    public UILabel textLabel; //required

    public Transform choiceContainer; //optional: each child must have a UIEventListener

    public event OnAction actionCallback;

    private struct ChoiceData {
        public UILabel label;
        public UIEventListener listener;
        public UIButtonKeys keys;
    }

    private ChoiceData[] mChoiceEvents = null;
    private int mNumChoices;
    private bool mIsInit = false;

    public static UIModalCharacterDialog Open(bool isLocalized, string modalRef, string text, string aName = null, string portraitSpriteRef = null, string[] choices = null) {
        UIModalManager ui = UIModalManager.instance;
        UIModalCharacterDialog dlg = ui.ModalGetController<UIModalCharacterDialog>(modalRef);

        if(dlg != null) {
            dlg.Apply(isLocalized, text, aName, portraitSpriteRef, choices);

            if(!ui.ModalIsInStack(modalRef)) {
                ui.ModalOpen(modalRef); //will show on the next update
            }
        }
        else {
            Debug.LogWarning("Failed to open dialog: " + modalRef);
        }

        return dlg;
    }
    
    /// <summary>
    /// Set the text and such for the dialog, call this before opening the dialog
    /// </summary>
    public void Apply(bool isLocalized, string text, string aName = null, string portraitSpriteRef = null, string[] choices = null) {
        InitData();

        textLabel.text = isLocalized ? GameLocalize.GetText(text) : text;

        if(nameLabel != null)
            nameLabel.text = isLocalized ? GameLocalize.GetText(aName) : aName;

        if(portrait != null) {
            if(portraitSpriteRef != null) {
                portrait.spriteName = portraitSpriteRef;
                portrait.gameObject.SetActive(true);
            }
            else {
                portrait.gameObject.SetActive(false);
            }
        }

        //apply choices
        ResetChoices();

        if(mChoiceEvents != null) {
            if(choices != null && choices.Length > 0) {
                mNumChoices = Mathf.Min(choices.Length, mChoiceEvents.Length);

                for(int i = 0; i < mNumChoices; i++) {
                    mChoiceEvents[i].keys.gameObject.SetActive(true);
                    if(mChoiceEvents[i].label != null) {
                        mChoiceEvents[i].label.text = isLocalized ? GameLocalize.GetText(choices[i]) : choices[i];
                    }
                    

                    if(i == 0) {
                        mChoiceEvents[i].keys.selectOnUp = mChoiceEvents[mNumChoices - 1].keys;
                    }
                    else {
                        mChoiceEvents[i].keys.selectOnUp = mChoiceEvents[i - 1].keys;
                    }

                    if(i < mNumChoices - 1) {
                        mChoiceEvents[i].keys.selectOnDown = mChoiceEvents[i + 1].keys;
                    }
                    else {
                        mChoiceEvents[i].keys.selectOnDown = mChoiceEvents[0].keys;
                    }
                }
            }
        }

        //if dialog is already open, apply positioning
        if(gameObject.activeSelf) {
            ApplyActive();

            NGUILayoutBase.RefreshNow(transform);
        }
    }

    void OnDestroy() {
        if(mChoiceEvents != null) {
            foreach(ChoiceData dat in mChoiceEvents) {
                if(dat.listener != null)
                    dat.listener.onClick = null;
            }
        }

        actionCallback = null;
    }

    void Awake() {
        InitData();
    }

    // Use this for initialization
    void Start() {

    }

    protected override void OnActive(bool active) {
        //for selector, activate first choice
        if(active) {
            ApplyActive();
        }
    }

    protected override void OnOpen() {
        NGUILayoutBase.RefreshNow(transform);
    }

    protected override void OnClose() {
        //reset choices
        ResetChoices();
    }

    void ApplyActive() {
        if(mChoiceEvents != null && mNumChoices > 0) {
            UICamera.selectedObject = mChoiceEvents[0].keys.gameObject;
        }
        else {
            UICamera.selectedObject = gameObject;
        }
    }

    //this would be called if we have no choice events, or a mouse click. If there are choices, ignore
    void OnClick() {
        if(mNumChoices == 0 && actionCallback != null) {
            actionCallback(-1);

        }
    }

    private void InitData() {
        if(!mIsInit) {
            if(portrait != null) {
                portrait.pivot = UIWidget.Pivot.TopLeft;
            }

            if(nameLabel != null) {
                nameLabel.pivot = UIWidget.Pivot.TopLeft;
            }

            if(textLabel != null) {
                textLabel.pivot = UIWidget.Pivot.TopLeft;
            }

            /*if(contentContainer != null) {
                mContentLayoutFlow = contentContainer.GetComponent<NGUILayoutFlow>();
                mContentLayoutFlow.arrangement = NGUILayoutFlow.Arrangement.Vertical;
                mContentLayoutFlow.rounding = true;
                mContentLayoutFlow.relativeLineup = false;

                mContentLayoutFlow.lineup = NGUILayoutFlow.LineUp.End;
                mContentLayoutFlow.lineup2 = NGUILayoutFlow.LineUp.Center;
            }*/

            /*if(bodyContainer != null) {
                if(mContentLayoutFlow != null) {
                    mContentLayoutFlow.lineup = NGUILayoutFlow.LineUp.None;
                    mContentLayoutFlow.lineup2 = NGUILayoutFlow.LineUp.None;
                }

                mBodyLayoutFlow = bodyContainer.GetComponent<NGUILayoutFlow>();
                mBodyLayoutFlow.arrangement = NGUILayoutFlow.Arrangement.Horizontal;
                mBodyLayoutFlow.rounding = true;
                mBodyLayoutFlow.relativeLineup = false;
                mBodyLayoutFlow.lineup = NGUILayoutFlow.LineUp.Center;
                mBodyLayoutFlow.lineup2 = NGUILayoutFlow.LineUp.End;
            }*/

            if(choiceContainer != null && choiceContainer.GetChildCount() > 0) {
                /*mChoiceFlow = choiceContainer.GetComponent<NGUILayoutFlow>();
                mChoiceFlow.arrangement = NGUILayoutFlow.Arrangement.Vertical;
                mChoiceFlow.rounding = true;
                mChoiceFlow.relativeLineup = false;
                mChoiceFlow.lineup = NGUILayoutFlow.LineUp.None;
                mChoiceFlow.lineup2 = NGUILayoutFlow.LineUp.None;*/

                mChoiceEvents = new ChoiceData[choiceContainer.GetChildCount()];

                //setup callback for click
                //disable all choices
                for(int i = 0; i < mChoiceEvents.Length; i++) {
                    Transform choice = choiceContainer.GetChild(i);
                    mChoiceEvents[i].listener = choice.GetComponent<UIEventListener>();
                    mChoiceEvents[i].listener.onClick = OnChoiceClick;

                    mChoiceEvents[i].keys = choice.GetComponent<UIButtonKeys>();

                    //only the first one, should only be one of these!
                    UILabel[] labels = choice.GetComponentsInChildren<UILabel>(true);
                    if(labels.Length > 0)
                        mChoiceEvents[i].label = labels[0];
                    //

                    choice.gameObject.SetActive(false);
                }
            }

            mIsInit = true;
        }
    }

    private void OnChoiceClick(GameObject go) {
        for(int i = 0; i < mChoiceEvents.Length; i++) {
            if(mChoiceEvents[i].listener.gameObject == go) {
                if(actionCallback != null)
                    actionCallback(i);

                break;
            }
        }
    }

    private void ResetChoices() {
        if(mChoiceEvents != null) {
            foreach(ChoiceData dat in mChoiceEvents) {
                dat.listener.gameObject.SetActive(false);
                dat.keys.selectOnUp = null;
                dat.keys.selectOnDown = null;
            }
        }

        mNumChoices = 0;
    }
}
