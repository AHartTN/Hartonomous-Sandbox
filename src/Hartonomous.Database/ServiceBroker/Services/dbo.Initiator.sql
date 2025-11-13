CREATE SERVICE [//Hartonomous/Service/Initiator] ON QUEUE InitiatorQueue (
        [//Hartonomous/AutonomousLoop/AnalyzeContract],
        [//Hartonomous/AutonomousLoop/HypothesizeContract],
        [//Hartonomous/AutonomousLoop/ActContract],
        [//Hartonomous/AutonomousLoop/LearnContract]
    );