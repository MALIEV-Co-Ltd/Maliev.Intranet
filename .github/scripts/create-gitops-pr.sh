#!/bin/bash
set -e

SERVICE_NAME=$1
ENVIRONMENT=$2
IMAGE_TAG=$3
SOURCE_BRANCH=$4
COMMIT_SHA=$5
GITOPS_REPO="MALIEV-Co-Ltd/maliev-gitops"
GITOPS_PATH="maliev-gitops"

echo "Creating Pull Request for $SERVICE_NAME in $ENVIRONMENT environment"

cd $GITOPS_PATH

git config --global user.name 'github-actions[bot]'
git config --global user.email 'github-actions[bot]@users.noreply.github.com'

# Extract release version if available
if [[ "$ENVIRONMENT" == "Staging" ]]; then
    RELEASE_VERSION=$(echo $SOURCE_BRANCH | sed -e 's,.*/v,,g')
    BRANCH_NAME="${SERVICE_NAME,,}/staging-$RELEASE_VERSION"
    COMMIT_MSG="chore(staging): Update $SERVICE_NAME image to $RELEASE_VERSION"
    PR_TITLE="chore(staging): Update $SERVICE_NAME"
    PR_BODY_EXTRA="- **Release**: $SOURCE_BRANCH"
else
    # Use 'dev' instead of 'development' for compatibility with gitops auto-merge
    ENV_SHORT=${ENVIRONMENT,,}
    if [[ "$ENV_SHORT" == "development" ]]; then ENV_SHORT="dev"; fi
    
    BRANCH_NAME="${SERVICE_NAME,,}/$ENV_SHORT-$COMMIT_SHA"
    COMMIT_MSG="chore($ENV_SHORT): Update $SERVICE_NAME image to $IMAGE_TAG"
    PR_TITLE="chore($ENV_SHORT): Update $SERVICE_NAME"
    PR_BODY_EXTRA=""
fi


# Create a new branch for this update
git checkout -b $BRANCH_NAME

# Commit and push changes to the new branch
git add .
if git diff --staged --quiet; then
    echo "No changes to commit"
    exit 0
fi

git commit -m "$COMMIT_MSG"
git push origin $BRANCH_NAME

# Prepare PR Body
PR_BODY=$(cat <<EOF
Automated GitOps Update$( [[ "$ENVIRONMENT" == "Production" ]] && echo " - **PRODUCTION**" )

- **Service**: $SERVICE_NAME
- **Environment**: $ENVIRONMENT
- **Image Tag**: $IMAGE_TAG
$PR_BODY_EXTRA
- **Source Branch**: $SOURCE_BRANCH
- **Commit**: $COMMIT_SHA

Updates $SERVICE_NAME image in ${ENVIRONMENT,,} overlay.
$( [[ "$ENVIRONMENT" == "Production" ]] && echo "**WARNING**: This is a production deployment and should be reviewed before merging." )
This PR was automatically created by GitHub Actions.
Once merged, ArgoCD will automatically sync the changes to the maliev-${ENVIRONMENT,,} namespace.

Triggered by: $GITHUB_ACTOR
Workflow: $GITHUB_WORKFLOW
Run: [$GITHUB_RUN_ID](https://github.com/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID)
EOF
)

# Create pull request using GitHub CLI
gh pr create \
    --title "$PR_TITLE" \
    --body "$PR_BODY" \
    --base main \
    --head $BRANCH_NAME \
    --repo $GITOPS_REPO
