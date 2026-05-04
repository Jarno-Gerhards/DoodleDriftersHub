export function getVotableSubmissions(submissions, localPlayerId) {
  const list = Array.isArray(submissions) ? submissions : [];
  return list.filter((item) => item && item.playerId !== localPlayerId);
}

export function createVotePayload({
  voterPlayerId,
  chosenPlayerId,
  submissions = []
}) {
  const chosenSubmission = submissions.find((item) => item.playerId === chosenPlayerId) || null;

  return {
    voterPlayerId,
    chosenPlayerId,
    chosenPlayerName: chosenSubmission ? chosenSubmission.playerName : null,
    votedAt: new Date().toISOString()
  };
}

export function dispatchVote(payload, eventName = "voting:vote-submitted") {
  window.dispatchEvent(new CustomEvent(eventName, { detail: payload }));
}
