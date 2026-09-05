using System;
using GrooveWarrior.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using YWJ.GameStateMachine;
using YWJ.GameLogic;


namespace YWJ.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("InputAble")]
        [SerializeField] private bool _isInputAble = false;

        [Header("Success Checker")]
        [SerializeField] private TabSuccessChecker _tabSuccessChecker;

        [Header("Rhythm Pause")]
        [SerializeField] private RhythmPauseController _rhythmPauseController;


        // player input events
        private PlayerInput _playerInput;

        private InputAction _tabAction;


        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();

            _tabSuccessChecker = FindFirstObjectByType<TabSuccessChecker>();
            _rhythmPauseController =
                FindFirstObjectByType<RhythmPauseController>();

            _tabAction = _playerInput.actions["Tab"];
        }

        private void OnEnable()
        {
            EnableInput();

            _tabAction.performed += OnTabInput;
            _tabAction.started += OnHoldTabStart;
            _tabAction.canceled += OnHoldTabCancel;

            if (_rhythmPauseController != null)
                _rhythmPauseController.Paused += OnRhythmPaused;
        }

        private void OnDisable()
        {
            DisableInput();

            _tabAction.performed -= OnTabInput;
            _tabAction.started -= OnHoldTabStart;
            _tabAction.canceled -= OnHoldTabCancel;

            if (_rhythmPauseController != null)
                _rhythmPauseController.Paused -= OnRhythmPaused;
        }



        // PressNote Action (Space)
        private void OnTabInput(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (!_isInputAble)
            {
                Debug.LogWarning("Input is disabled. Tab action ignored.");
                return;
            }

            if (IsRhythmPaused())
                return;

            Debug.Log("Tab action performed!");

            _tabSuccessChecker.CheckTabSuccess();
        }

        private void OnHoldTabStart(InputAction.CallbackContext context)
        {
            if (!context.started)
            {
                return;
            }

            if (!_isInputAble)
            {
                Debug.LogWarning("Input is disabled. Hold Tab action ignored.");
                return;
            }

            if (IsRhythmPaused())
                return;

            Debug.Log("Hold Tab started!");

            _tabSuccessChecker.EnableHoldingTab();
            _tabSuccessChecker.CheckStartHoldTabSuccess();
        }

        private void OnHoldTabCancel(InputAction.CallbackContext context)
        {
            if (!context.canceled)
            {
                return;
            }

            if (!_isInputAble)
            {
                Debug.LogWarning("Input is disabled. Hold Tab action ignored.");
                return;
            }

            if (IsRhythmPaused())
                return;

            Debug.Log("Hold Tab ended!");

            _tabSuccessChecker.DisableHoldingTab();
            _tabSuccessChecker.CheckCancelHoldTabSuccess();
        }



        // Input control methods
        private void EnableInput()
        {
            _isInputAble = true;
        }

        private void DisableInput()
        {
            _isInputAble = false;
        }

        private bool IsRhythmPaused()
        {
            if (_rhythmPauseController == null)
            {
                _rhythmPauseController =
                    FindFirstObjectByType<RhythmPauseController>();
            }

            return _rhythmPauseController != null &&
                _rhythmPauseController.IsPaused;
        }

        private void OnRhythmPaused()
        {
            _tabSuccessChecker?.DisableHoldingTab();
        }
    }
}
