# Sourced by deploy-api-demo.sh and deploy-api-prod-v2.sh (expects REPO_ROOT and the SSH array).
# Only the pushed, tested tip of main may leave this machine; prod also requires the demo to run it.

API_PATHS=(api-perle-rare-decompiled)

# Replaces remote dir $2 with the contents of local dir $1. Only needs tar + ssh (Git Bash on Windows has no rsync).
# COPYFILE_DISABLE stops macOS tar from shipping ._* metadata files. Windows drops the exec bit: restore it on the server.
upload_dir() {
	local src="$1" dest="$2"
	COPYFILE_DISABLE=1 tar -czf - -C "$src" . \
		| "${SSH[@]}" "rm -rf '$dest' && mkdir -p '$dest' && tar --warning=no-unknown-keyword -xzf - -C '$dest' && chmod +x '$dest/ApiPerleRare'"
}

guard_pushed_main() {
	local branch
	branch="$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD)"
	if [[ "$branch" != "main" ]]; then
		echo "Refused: on branch '$branch' — deploys ship main only." >&2
		exit 1
	fi
	if [[ -n "$(git -C "$REPO_ROOT" status --porcelain -- "${API_PATHS[@]}" tests infra/ovh/systemd infra/ovh/apache)" ]]; then
		echo "Refused: uncommitted changes in the API, tests or infra (deploy what is on main)." >&2
		exit 1
	fi
	git -C "$REPO_ROOT" fetch -q origin main
	if [[ "$(git -C "$REPO_ROOT" rev-parse HEAD)" != "$(git -C "$REPO_ROOT" rev-parse origin/main)" ]]; then
		echo "Refused: HEAD is not origin/main — pull, then push, before deploying." >&2
		exit 1
	fi
}

# A user-local SDK in ~/.dotnet wins over an older system one; elsewhere (Windows installer) leave dotnet as found.
use_local_dotnet() {
	if [[ -x "$HOME/.dotnet/dotnet" ]]; then
		export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH"
	fi
}

guard_tests() {
	echo "==> Regression guard: build + unit tests"
	use_local_dotnet
	(cd "$REPO_ROOT" && dotnet build ApiPerleRare.sln -c Release && dotnet test tests/ApiPerleRare.Tests -c Release --no-build) || {
		echo "Refused: build or tests are red." >&2
		exit 1
	}
}

# $1 = commit recorded by the last deploy-api-demo.sh (empty if none).
guard_demo_validated() {
	local demo_sha="$1"
	if [[ -z "$demo_sha" ]] || ! git -C "$REPO_ROOT" cat-file -e "${demo_sha}^{commit}" 2>/dev/null; then
		echo "Refused: no usable API demo deploy recorded ('$demo_sha') — run go demo first." >&2
		exit 1
	fi
	if ! git -C "$REPO_ROOT" diff --quiet "$demo_sha" HEAD -- "${API_PATHS[@]}"; then
		echo "Refused: this API version never ran on the demo (demo=$demo_sha) — run go demo first." >&2
		exit 1
	fi
}
