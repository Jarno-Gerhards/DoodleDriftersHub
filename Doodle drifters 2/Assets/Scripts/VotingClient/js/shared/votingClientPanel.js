import {
  createVotePayload,
  dispatchVote,
  getVotableSubmissions
} from "../core/votingLogic.js";

export class VotingClientPanel {
  constructor(options) {
    const {
      gridElement,
      statusElement,
      localPlayerId = "player-local",
      onVoteSubmitted = null
    } = options || {};

    if (!gridElement) {
      throw new Error("[VotingClientPanel] gridElement is required");
    }

    this.gridElement = gridElement;
    this.statusElement = statusElement || null;
    this.localPlayerId = localPlayerId;
    this.onVoteSubmitted = onVoteSubmitted;

    this.submissions = [];
    this.hasVoted = false;
    this.selectedPlayerId = null;
  }

  setLocalPlayer(playerId) {
    this.localPlayerId = playerId || this.localPlayerId;
  }

  setSubmissions(submissions) {
    this.submissions = getVotableSubmissions(submissions, this.localPlayerId);
    this.hasVoted = false;
    this.selectedPlayerId = null;
    this.render();
    this.setStatus("Tap a drawing to cast your vote.");
  }

  lockWithWinner(winnerPlayerId) {
    const cards = this.gridElement.querySelectorAll(".vote-card");
    cards.forEach((button) => {
      button.disabled = true;
      button.classList.toggle("is-selected", button.dataset.playerId === winnerPlayerId);
    });

    const winner = this.submissions.find((item) => item.playerId === winnerPlayerId);
    this.setStatus(winner
      ? "The party chose: " + winner.playerName + "!"
      : "Voting complete.");
  }

  render() {
    this.gridElement.innerHTML = "";

    this.submissions.forEach((submission) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "vote-card";
      button.dataset.playerId = submission.playerId;
      button.setAttribute("aria-label", "Vote for " + submission.playerName);

      if (submission.imageDataUrl) {
        const image = document.createElement("img");
        image.src = submission.imageDataUrl;
        image.alt = submission.playerName;
        button.appendChild(image);
      }

      button.addEventListener("click", () => this.handleCardClicked(submission.playerId));
      this.gridElement.appendChild(button);
    });
  }

  handleCardClicked(chosenPlayerId) {
    if (this.hasVoted) {
      return;
    }

    this.selectedPlayerId = chosenPlayerId;
    this.applySelection();
    this.submitVote(chosenPlayerId);
  }

  applySelection() {
    const cards = this.gridElement.querySelectorAll(".vote-card");
    cards.forEach((button) => {
      button.classList.toggle("is-selected", button.dataset.playerId === this.selectedPlayerId);
    });
  }

  submitVote(chosenPlayerId) {
    if (!chosenPlayerId || this.hasVoted) {
      return;
    }

    this.hasVoted = true;

    const payload = createVotePayload({
      voterPlayerId: this.localPlayerId,
      chosenPlayerId,
      submissions: this.submissions
    });

    if (typeof this.onVoteSubmitted === "function") {
      this.onVoteSubmitted(payload);
    } else {
      dispatchVote(payload);
    }

    this.setStatus("Vote submitted! Waiting for other players...");

    const cards = this.gridElement.querySelectorAll(".vote-card");
    cards.forEach((button) => {
      button.disabled = true;
    });
  }

  setStatus(message) {
    if (this.statusElement) {
      this.statusElement.textContent = message;
    }
  }
}
