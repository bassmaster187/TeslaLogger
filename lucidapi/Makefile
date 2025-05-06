PYTHON=python

PROTO_DIR=proto
GEN_DIR=lucidmotors/gen


ifeq ($(V),1)
Q=
else
Q=@
endif


.PHONY: usage
usage:
	@echo "Usage:"
	@echo "  make requirements  - Regenerate requirements.txt files with Poetry"
	@echo "  make protobuf      - Compile gRPC/ProtoBuf definitions"
	@echo "  make clean         - Clean build artifacts"


#
# Generate requirements files for pip from pyproject.toml
#

requirements.txt: pyproject.toml
	@echo EXPORT $@
	$(Q)$(PYTHON) -m poetry export -o requirements.txt --without-hashes

requirements_dev.txt: pyproject.toml
	@echo EXPORT $@
	$(Q)$(PYTHON) -m poetry export -o requirements_dev.txt --without-hashes --with dev

.PHONY: requirements
requirements: requirements.txt requirements_dev.txt


#
# Compile gRPC/ProtoBuf definitions
#

PROTOS= login_session.proto            \
	user_profile_service.proto     \
	user_preferences_service.proto \
	vehicle_state_service.proto    \
	trip_service.proto             \
	charging_service.proto         \
	sentry_service.proto           \
	subscription_service.proto     \
	salesforce_service.proto

PROTOS_GEN := $(patsubst %.proto,%_pb2.py,$(PROTOS))      \
	      $(patsubst %.proto,%_pb2_grpc.py,$(PROTOS)) \
	      $(patsubst %.proto,%_pb2.pyi,$(PROTOS))

PROTOS := $(addprefix $(PROTO_DIR)/,$(PROTOS))
PROTOS_GEN := $(addprefix $(GEN_DIR)/,$(PROTOS_GEN))

# We have some sed insanity here because:
# 1) protoc doesn't support generating relative imports
# 2) https://github.com/python/mypy/issues/10870
$(GEN_DIR)/%_pb2.py $(GEN_DIR)/%_pb2_grpc.py $(GEN_DIR)/%_pb2.pyi: $(PROTO_DIR)/%.proto
	@echo PROTOC $*
	$(Q)mkdir -p $(GEN_DIR)
	$(Q)touch $(GEN_DIR)/__init__.py
	$(Q)$(PYTHON) -m grpc_tools.protoc -I $(PROTO_DIR) --python_out=$(GEN_DIR) --pyi_out=$(GEN_DIR) --grpc_python_out=$(GEN_DIR) $<
	$(Q)sed -i.orig 's/^import \([a-zA-Z0-9_]*_pb2\)/from . import \1/;s/^\(\s*\)__slots__ = \[\]$$/\1__slots__ = ()/' $(GEN_DIR)/$*_pb2.py $(GEN_DIR)/$*_pb2_grpc.py $(GEN_DIR)/$*_pb2.pyi

.PHONY: protobuf
protobuf: $(PROTOS_GEN)

.PHONY: clean-protos
clean-protos:
	$(Q)rm -rf $(GEN_DIR)



.PHONY: clean
clean: clean-protos
